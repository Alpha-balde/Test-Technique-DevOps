terraform {
  required_version = ">= 1.11.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.9"
    }
    time = {
      source  = "hashicorp/time"
      version = "~> 0.14"
    }
  }

  # Local state for this test (git-ignored). It holds no secret: passwords and
  # the connection string only go through write-only arguments.
  # Production: azurerm backend (Storage Account, Entra ID auth, versioning, locking).
}
