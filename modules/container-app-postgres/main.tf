data "azurerm_client_config" "current" {}

# Suffix for names that must be globally unique (Key Vault, PostgreSQL, DNS zone).
resource "random_string" "suffix" {
  length  = 6
  lower   = true
  upper   = false
  numeric = true
  special = false
}

locals {
  name   = "${var.project_name}-${var.environment}"
  suffix = random_string.suffix.result

  # Name length budget (inputs are validated: project_name <= 12, environment <= 12, suffix = 6):
  #   Key Vault (3-24)       : "kv-"(3) + project(12) + "-"(1) + suffix(6)                    = 22 max
  #   PostgreSQL (3-63)      : "psql-"(5) + project(12) + "-"(1) + env(12) + "-"(1) + suffix(6) = 37 max
  #   Container App (2-32)   : "ca-"(3) + project(12) + "-"(1) + env(12)                       = 28 max
  # The environment is not part of the Key Vault name (24-char limit); it stays unique
  # thanks to the random suffix and is identifiable through its resource group and tags.
  key_vault_name       = "kv-${var.project_name}-${local.suffix}"
  postgres_server_name = "psql-${local.name}-${local.suffix}"
  container_app_name   = "ca-${local.name}"

  tags = merge(var.tags, {
    project     = var.project_name
    environment = var.environment
    managed-by  = "terraform"
  })
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.name}"
  location = var.location
  tags     = local.tags
}

# Identity of the application, used to read its secrets from Key Vault.
resource "azurerm_user_assigned_identity" "app" {
  name                = "id-${local.name}"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  tags                = local.tags
}
