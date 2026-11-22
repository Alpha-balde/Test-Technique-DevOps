variable "project_name" {
  description = "Short project name (see module validation)."
  type        = string
}

variable "environment" {
  description = "Environment name."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "container_image" {
  description = "Image to deploy, pinned by tag or digest."
  type        = string
}

variable "tags" {
  description = "Additional tags."
  type        = map(string)
  default     = {}
}
