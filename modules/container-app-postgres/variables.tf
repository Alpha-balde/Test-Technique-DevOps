# ---------------------------------------------------------------------------
# Required inputs
# ---------------------------------------------------------------------------

variable "project_name" {
  description = "Short project name used in every resource name. Lower-case letters, digits and single hyphens, 2 to 12 characters."
  type        = string

  validation {
    condition     = can(regex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$", var.project_name)) && length(var.project_name) >= 2 && length(var.project_name) <= 12
    error_message = "project_name must be 2-12 characters: lower-case letters, digits and single hyphens, starting with a letter and not ending with a hyphen."
  }
}

variable "environment" {
  description = "Environment name (e.g. staging, production). Lower-case letters, digits and single hyphens, 2 to 12 characters."
  type        = string

  validation {
    condition     = can(regex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$", var.environment)) && length(var.environment) >= 2 && length(var.environment) <= 12
    error_message = "environment must be 2-12 characters: lower-case letters, digits and single hyphens, starting with a letter and not ending with a hyphen."
  }
}

variable "location" {
  description = "Azure region where every resource is created (e.g. francecentral)."
  type        = string
}

variable "container_image" {
  description = "Full reference of the image to deploy, pinned by tag or digest (e.g. ghcr.io/org/app:sha-1234567). The image must be pullable anonymously."
  type        = string

  validation {
    condition = (
      can(regex("^[^[:space:]]+(:[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}|@sha256:[a-f0-9]{64})$", var.container_image))
      && !endswith(var.container_image, ":latest")
    )
    error_message = "container_image must include an explicit tag or digest, and must not use the mutable 'latest' tag."
  }
}

# ---------------------------------------------------------------------------
# Optional inputs (sensible defaults)
# ---------------------------------------------------------------------------

variable "tags" {
  description = "Additional tags applied to every resource."
  type        = map(string)
  default     = {}
}

variable "vnet_address_space" {
  description = "Address space of the virtual network."
  type        = list(string)
  default     = ["10.20.0.0/16"]
}

variable "apps_subnet_prefix" {
  description = "CIDR of the subnet delegated to the Container Apps environment (workload profiles: /27 minimum)."
  type        = string
  default     = "10.20.0.0/24"

  validation {
    condition     = can(cidrhost(var.apps_subnet_prefix, 0)) && tonumber(split("/", var.apps_subnet_prefix)[1]) <= 27
    error_message = "apps_subnet_prefix must be a valid CIDR of size /27 or larger."
  }
}

variable "postgres_subnet_prefix" {
  description = "CIDR of the subnet delegated to PostgreSQL Flexible Server."
  type        = string
  default     = "10.20.1.0/27"

  validation {
    condition     = can(cidrhost(var.postgres_subnet_prefix, 0)) && tonumber(split("/", var.postgres_subnet_prefix)[1]) <= 28
    error_message = "postgres_subnet_prefix must be a valid CIDR of size /28 or larger."
  }
}

variable "postgres_version" {
  description = "PostgreSQL major version."
  type        = string
  default     = "17"
}

variable "postgres_sku_name" {
  description = "PostgreSQL Flexible Server SKU (tier_name)."
  type        = string
  default     = "B_Standard_B1ms"
}

variable "postgres_storage_mb" {
  description = "PostgreSQL storage size in MB."
  type        = number
  default     = 32768
}

variable "postgres_backup_retention_days" {
  description = "PostgreSQL backup retention in days (7-35)."
  type        = number
  default     = 7

  validation {
    condition     = var.postgres_backup_retention_days >= 7 && var.postgres_backup_retention_days <= 35
    error_message = "postgres_backup_retention_days must be between 7 and 35."
  }
}

variable "db_name" {
  description = "Name of the application database. Changing it requires incrementing db_password_version so the connection string secret is rewritten."
  type        = string
  default     = "deployrequests"
}

variable "db_admin_login" {
  description = "PostgreSQL administrator login used by the application."
  type        = string
  default     = "dbadmin"

  validation {
    condition = (
      can(regex("^[a-z][a-z0-9_]{0,62}$", var.db_admin_login))
      && !startswith(var.db_admin_login, "pg_")
      && !contains(["azure_superuser", "azure_pg_admin", "admin", "administrator", "root", "guest", "public"], var.db_admin_login)
    )
    error_message = "db_admin_login must be a valid PostgreSQL identifier, must not start with 'pg_' and must not be a reserved Azure name."
  }
}

variable "db_password_version" {
  # Write-only values (*_wo) are only sent to Azure when this version changes.
  # Increment it when: rotating the password, changing db_name or db_admin_login,
  # or changing the connection string format in secrets.tf. See the module README.
  description = "Version of the write-only secrets. Increment it to rotate the password or to rewrite the connection string (db_name, db_admin_login or format change): both Key Vault secrets and the server password are rewritten together."
  type        = number
  default     = 1

  validation {
    condition     = var.db_password_version >= 1 && floor(var.db_password_version) == var.db_password_version
    error_message = "db_password_version must be a positive integer."
  }
}

variable "key_vault_soft_delete_retention_days" {
  description = "Key Vault soft-delete retention (7-90 days)."
  type        = number
  default     = 7
}

variable "rbac_propagation_wait" {
  description = "Time to wait after the Key Vault role assignments so they propagate before secrets are written and read."
  type        = string
  default     = "300s"
}

variable "container_port" {
  description = "Port the container listens on."
  type        = number
  default     = 8080
}

variable "container_cpu" {
  description = "vCPU allocated to the container (must form a valid Consumption pair with container_memory)."
  type        = number
  default     = 0.5
}

variable "container_memory" {
  description = "Memory allocated to the container (e.g. 1Gi)."
  type        = string
  default     = "1Gi"
}

variable "min_replicas" {
  description = "Minimum number of replicas (1 avoids cold starts)."
  type        = number
  default     = 1
}

variable "max_replicas" {
  description = "Maximum number of replicas."
  type        = number
  default     = 2

  validation {
    condition     = var.max_replicas >= 1
    error_message = "max_replicas must be at least 1."
  }
}

variable "connection_string_env_name" {
  description = "Environment variable through which the application receives the database connection string."
  type        = string
  default     = "ConnectionStrings__Database"
}

variable "app_settings" {
  description = "Additional non-secret environment variables for the container. Must not redefine the connection string variable."
  type        = map(string)
  default     = {}

  validation {
    condition     = !contains([for name in keys(var.app_settings) : upper(name)], upper(var.connection_string_env_name))
    error_message = "app_settings must not redefine the connection string variable (connection_string_env_name): it is injected from Key Vault."
  }
}

variable "health_path" {
  description = "Health endpoint including dependencies (readiness probe)."
  type        = string
  default     = "/health"
}

variable "liveness_path" {
  description = "Process-only health endpoint, without dependencies (startup and liveness probes)."
  type        = string
  default     = "/health/live"
}

variable "startup_probe_failure_threshold" {
  description = "Startup probe failures tolerated (every 10s). Must cover the app's worst-case migration retries (~334s)."
  type        = number
  default     = 40
}
