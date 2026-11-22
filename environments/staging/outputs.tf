output "app_url" {
  description = "Public HTTPS URL of the application."
  value       = module.app.app_url
}

output "health_url" {
  description = "Health endpoint (includes the database check)."
  value       = module.app.health_url
}

output "resource_group_name" {
  description = "Resource group of the environment."
  value       = module.app.resource_group_name
}

output "container_app_name" {
  description = "Name of the Container App."
  value       = module.app.container_app_name
}

output "key_vault_name" {
  description = "Key Vault holding the secrets."
  value       = module.app.key_vault_name
}

output "postgres_server_fqdn" {
  description = "Private FQDN of the PostgreSQL server."
  value       = module.app.postgres_server_fqdn
}

output "db_admin_login" {
  description = "PostgreSQL administrator login."
  value       = module.app.db_admin_login
  sensitive   = true
}

output "db_password_secret_id" {
  description = "Key Vault URL of the database password secret."
  value       = module.app.db_password_secret_id
  sensitive   = true
}

output "db_connection_string_secret_id" {
  description = "Key Vault URL of the connection string secret."
  value       = module.app.db_connection_string_secret_id
  sensitive   = true
}
