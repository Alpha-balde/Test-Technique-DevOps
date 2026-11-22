# Subscription: taken from the Azure CLI session (`az account show`) or the
# ARM_SUBSCRIPTION_ID environment variable. Nothing is hard-coded.
provider "azurerm" {
  features {
    key_vault {
      # Lets `terraform destroy` leave nothing behind (no soft-deleted vault).
      purge_soft_delete_on_destroy = true
    }
  }

  # AzureRM 5.x no longer registers resource providers by default:
  # register exactly those this configuration needs, before plan/apply.
  resource_provider_registrations = "none"
  resource_providers_to_register = [
    "Microsoft.App",
    "Microsoft.DBforPostgreSQL",
    "Microsoft.KeyVault",
    "Microsoft.ManagedIdentity",
    "Microsoft.Network",
  ]
}
