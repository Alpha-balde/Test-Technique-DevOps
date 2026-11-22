# Input validation tests. Providers are mocked: no Azure call, no credentials.
# Run from the module folder: terraform init && terraform test

mock_provider "azurerm" {
  mock_data "azurerm_client_config" {
    defaults = {
      tenant_id       = "00000000-0000-0000-0000-000000000001"
      object_id       = "00000000-0000-0000-0000-000000000002"
      client_id       = "00000000-0000-0000-0000-000000000003"
      subscription_id = "00000000-0000-0000-0000-000000000004"
    }
  }
}
mock_provider "random" {}
mock_provider "time" {}

variables {
  project_name    = "deployreq"
  environment     = "staging"
  location        = "francecentral"
  container_image = "ghcr.io/org/app:sha-1234567"
}

run "longest_names_fit_azure_limits" {
  command = plan

  variables {
    project_name = "abcdefghijkl" # 12 chars, the maximum
    environment  = "preprod-eu-1" # 12 chars, the maximum
  }

  override_resource {
    target          = random_string.suffix
    values          = { result = "x1y2z3" }
    override_during = plan
  }

  assert {
    condition     = azurerm_key_vault.this.name == "kv-abcdefghijkl-x1y2z3" && length(azurerm_key_vault.this.name) <= 24
    error_message = "Key Vault name must keep the full suffix and fit in 24 characters."
  }

  assert {
    condition     = length(azurerm_container_app.this.name) <= 32
    error_message = "Container App name must fit in 32 characters."
  }

  assert {
    condition     = length(azurerm_postgresql_flexible_server.this.name) <= 63
    error_message = "PostgreSQL server name must fit in 63 characters."
  }
}

run "null_postgres_sku_uses_module_default" {
  command = plan

  variables {
    postgres_sku_name = null
  }

  assert {
    condition     = azurerm_postgresql_flexible_server.this.sku_name == "B_Standard_B1ms"
    error_message = "A null postgres_sku_name must fall back to the module default."
  }
}

run "rejects_latest_tag" {
  command = plan

  variables {
    container_image = "ghcr.io/org/app:latest"
  }

  expect_failures = [var.container_image]
}

run "rejects_untagged_image" {
  command = plan

  variables {
    container_image = "ghcr.io/org/app"
  }

  expect_failures = [var.container_image]
}

run "rejects_connection_string_override_in_app_settings" {
  command = plan

  variables {
    app_settings = { connectionstrings__database = "Host=evil" }
  }

  expect_failures = [var.app_settings]
}

run "rejects_invalid_project_name" {
  command = plan

  variables {
    project_name = "Deploy--Req"
  }

  expect_failures = [var.project_name]
}

run "rejects_too_long_environment" {
  command = plan

  variables {
    environment = "production-europe"
  }

  expect_failures = [var.environment]
}
