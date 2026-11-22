resource "azurerm_postgresql_flexible_server" "this" {
  name                = local.postgres_server_name
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  version             = var.postgres_version
  sku_name            = var.postgres_sku_name
  storage_mb          = var.postgres_storage_mb

  backup_retention_days        = var.postgres_backup_retention_days
  geo_redundant_backup_enabled = false

  # Private access only: reachable from the VNet, never from the Internet.
  delegated_subnet_id           = azurerm_subnet.postgres.id
  private_dns_zone_id           = azurerm_private_dns_zone.postgres.id
  public_network_access_enabled = false

  administrator_login               = var.db_admin_login
  administrator_password_wo         = ephemeral.azurerm_key_vault_secret.db_password.value
  administrator_password_wo_version = var.db_password_version

  authentication {
    password_auth_enabled         = true
    active_directory_auth_enabled = false
  }

  tags = local.tags

  # The private DNS zone must be linked to the VNet (and the subnet secured)
  # before the server is created inside it.
  depends_on = [
    azurerm_private_dns_zone_virtual_network_link.postgres,
    azurerm_subnet_network_security_group_association.postgres,
  ]
}

resource "azurerm_postgresql_flexible_server_database" "app" {
  name      = var.db_name
  server_id = azurerm_postgresql_flexible_server.this.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}
