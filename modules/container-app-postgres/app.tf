resource "azurerm_container_app_environment" "this" {
  name                               = "cae-${local.name}"
  location                           = azurerm_resource_group.this.location
  resource_group_name                = azurerm_resource_group.this.name
  infrastructure_subnet_id           = azurerm_subnet.apps.id
  infrastructure_resource_group_name = "rg-${local.name}-cae-infra"
  internal_load_balancer_enabled     = false
  tags                               = local.tags

  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
  }
}

locals {
  connection_string_secret_name = "db-connection-string"
}

resource "azurerm_container_app" "this" {
  name                         = local.container_app_name
  container_app_environment_id = azurerm_container_app_environment.this.id
  resource_group_name          = azurerm_resource_group.this.name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"
  tags                         = local.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app.id]
  }

  # Key Vault reference: the container app only knows the secret URL. The
  # version-less URL lets a rotated secret be picked up without redeploying.
  secret {
    name                = local.connection_string_secret_name
    identity            = azurerm_user_assigned_identity.app.id
    key_vault_secret_id = azurerm_key_vault_secret.db_connection_string.versionless_id
  }

  ingress {
    external_enabled           = true
    target_port                = var.container_port
    transport                  = "auto"
    allow_insecure_connections = false # HTTPS only

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "app"
      image  = var.container_image
      cpu    = var.container_cpu
      memory = var.container_memory

      env {
        name        = var.connection_string_env_name
        secret_name = local.connection_string_secret_name
      }

      dynamic "env" {
        for_each = var.app_settings
        content {
          name  = env.key
          value = env.value
        }
      }

      # Startup: tolerates the app's worst-case migration retries
      # (10 attempts x ~28s + 9 x 6s = ~334s) -> 10s + 40 x 10s = ~410s.
      startup_probe {
        transport               = "HTTP"
        port                    = var.container_port
        path                    = var.liveness_path
        initial_delay           = 10
        interval_seconds        = 10
        timeout                 = 3
        failure_count_threshold = var.startup_probe_failure_threshold
      }

      # Liveness: process only, so a database outage never restarts the container.
      liveness_probe {
        transport               = "HTTP"
        port                    = var.container_port
        path                    = var.liveness_path
        interval_seconds        = 10
        timeout                 = 3
        failure_count_threshold = 3
      }

      # Readiness: includes the database (/health answers in <= ~4s when it is down).
      readiness_probe {
        transport               = "HTTP"
        port                    = var.container_port
        path                    = var.health_path
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 3
        success_count_threshold = 1
      }
    }
  }

  depends_on = [
    time_sleep.rbac_propagation,
    azurerm_postgresql_flexible_server_database.app,
  ]

  lifecycle {
    precondition {
      condition     = var.min_replicas <= var.max_replicas
      error_message = "min_replicas must be lower than or equal to max_replicas."
    }
  }
}
