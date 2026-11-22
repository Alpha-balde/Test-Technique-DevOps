terraform {
  # Write-only arguments (*_wo) and ephemeral resources require Terraform 1.11+.
  required_version = ">= 1.11.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 5.14.0, < 6.0.0"
    }
    random = {
      source  = "hashicorp/random"
      version = ">= 3.7.0, < 4.0.0"
    }
    time = {
      source  = "hashicorp/time"
      version = ">= 0.9.0, < 1.0.0"
    }
  }
}
