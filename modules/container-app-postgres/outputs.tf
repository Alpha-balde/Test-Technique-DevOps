output "app_url" {
  description = "Public HTTPS URL of the application."
  value       = "https://${azurerm_container_app.this.ingress[0].fqdn}"
}

output "health_url" {
  description = "Health endpoint (includes the database check)."
  value       = "https://${azurerm_container_app.this.ingress[0].fqdn}${var.health_path}"
}

output "resource_group_name" {
  description = "Resource group containing every resource of this environment."
  value       = azurerm_resource_group.this.name
}

output "container_app_name" {
  description = "Name of the Container App."
  value       = azurerm_container_app.this.name
}

output "key_vault_name" {
  description = "Name of the Key Vault holding the secrets."
  value       = azurerm_key_vault.this.name
}

output "postgres_server_fqdn" {
  description = "Private FQDN of the PostgreSQL server (resolvable only inside the VNet)."
  value       = azurerm_postgresql_flexible_server.this.fqdn
}

output "db_admin_login" {
  description = "PostgreSQL administrator login."
  value       = var.db_admin_login
  sensitive   = true
}

output "db_password_secret_id" {
  description = "Key Vault URL of the database password secret (the value itself is never exposed)."
  value       = azurerm_key_vault_secret.db_password.versionless_id
  sensitive   = true
}

output "db_connection_string_secret_id" {
  description = "Key Vault URL of the connection string secret (the value itself is never exposed)."
  value       = azurerm_key_vault_secret.db_connection_string.versionless_id
  sensitive   = true
}
