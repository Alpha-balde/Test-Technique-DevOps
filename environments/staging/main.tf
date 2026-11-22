module "app" {
  source = "../../modules/container-app-postgres"

  project_name    = var.project_name
  environment     = var.environment
  location        = var.location
  container_image = var.container_image
  tags            = var.tags

  postgres_sku_name = var.postgres_sku_name
}
