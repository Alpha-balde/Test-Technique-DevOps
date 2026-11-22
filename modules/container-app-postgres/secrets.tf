# Secrets are generated and passed through write-only arguments (*_wo):
# no password or connection string is ever stored in the Terraform plan or state.
# Key Vault is the single source of truth for the password: if an apply fails
# half-way and is re-run, PostgreSQL and the connection string read the value
# already stored in Key Vault instead of a newly generated one.

resource "azurerm_key_vault" "this" {
  name                       = local.key_vault_name
  location                   = azurerm_resource_group.this.location
  resource_group_name        = azurerm_resource_group.this.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  soft_delete_retention_days = var.key_vault_soft_delete_retention_days

  # Purge protection off so that `terraform destroy` can purge the vault.
  purge_protection_enabled = false

  # Data-plane access requires Entra ID authentication + RBAC role. A private
  # endpoint would block the deployer's workstation (see README, next steps).
  public_network_access_enabled = true

  tags = local.tags

  lifecycle {
    precondition {
      condition     = length(local.key_vault_name) <= 24
      error_message = "Key Vault name '${local.key_vault_name}' exceeds 24 characters."
    }
  }
}

# The identity running Terraform writes the secrets.
resource "azurerm_role_assignment" "deployer_secrets_officer" {
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# The application identity can only read secrets.
resource "azurerm_role_assignment" "app_secrets_user" {
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.app.principal_id
  principal_type       = "ServicePrincipal"
}

# RBAC assignments take time to propagate; writing or reading a secret too
# early returns 403. Wait before touching secrets (and before the app reads them).
resource "time_sleep" "rbac_propagation" {
  create_duration = var.rbac_propagation_wait

  depends_on = [
    azurerm_role_assignment.deployer_secrets_officer,
    azurerm_role_assignment.app_secrets_user,
  ]
}

ephemeral "random_password" "db" {
  length      = 40
  special     = false # avoids characters that would need escaping in the connection string
  min_lower   = 1
  min_upper   = 1
  min_numeric = 1
}

resource "azurerm_key_vault_secret" "db_password" {
  name             = "db-password"
  key_vault_id     = azurerm_key_vault.this.id
  value_wo         = ephemeral.random_password.db.result
  value_wo_version = var.db_password_version
  content_type     = "text/plain"
  tags             = local.tags

  depends_on = [time_sleep.rbac_propagation]
}

# Reads back the stored password (never persisted) for PostgreSQL and the connection string.
ephemeral "azurerm_key_vault_secret" "db_password" {
  name         = azurerm_key_vault_secret.db_password.name
  key_vault_id = azurerm_key_vault.this.id
  version      = azurerm_key_vault_secret.db_password.version
}

resource "azurerm_key_vault_secret" "db_connection_string" {
  name         = "db-connection-string"
  key_vault_id = azurerm_key_vault.this.id
  value_wo = join(";", [
    "Host=${azurerm_postgresql_flexible_server.this.fqdn}",
    "Port=5432",
    "Database=${azurerm_postgresql_flexible_server_database.app.name}",
    "Username=${var.db_admin_login}",
    "Password=${ephemeral.azurerm_key_vault_secret.db_password.value}",
    "SSL Mode=Require",
    "Timeout=5",
  ])
  value_wo_version = var.db_password_version
  content_type     = "text/plain"
  tags             = local.tags

  depends_on = [time_sleep.rbac_propagation]
}
