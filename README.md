# Demandes de déploiement : test DevOps

**Architecture** : une image Docker unique (API .NET 10 et écran Angular) tourne sur Azure Container Apps, lit ses
secrets dans Key Vault par identité managée et joint par un VNet un serveur PostgreSQL Flexible 17 privé. Un doublon
(application, environnement, version) est refusé en **HTTP 409** avec un message à l'écran. **Objectif** : tout
déployer par un seul `terraform apply`, à confirmer au prochain essai depuis zéro.

## Démarche

1. **API d'abord** : migration EF Core versionnée, unicité garantie en base, tests sur PostgreSQL réel.
2. **Une seule image Docker** (API et écran Angular), publiée sur GHCR et épinglée par digest.
3. **Module Terraform** et `staging` : `fmt`, `validate`, tests du module, relecture de chaque plan.
4. **Vérification en ligne** : `/health`, création d'une demande, doublon refusé en 409.
 **URL:** https://ca-deployreq-staging.nicetree-a3075cd2.northeurope.azurecontainerapps.io
## Choix et compromis

- **Réseau** : PostgreSQL privé ; le NSG autorise le port 5432 depuis le sous-réseau applicatif et dans le sous-réseau PostgreSQL, et refuse le reste du trafic du VNet.
- **Secrets** : mot de passe généré par une ressource Terraform éphémère, stocké dans Key Vault, transmis par des arguments *write-only* : **aucun secret dans le dépôt, le plan ou l'état**.
- **Compromis** : état local ; compte admin PostgreSQL ; Key Vault public protégé par RBAC ; attente RBAC de 300 s.
- **Abonnement** : North Europe (France Central et West Europe refusaient PostgreSQL) ; quota actuel : un environnement Container Apps.

## Déployer et supprimer

```powershell
# Prérequis : Terraform ≥ 1.11, Azure CLI, rôle Owner sur l'abonnement
az login
cd environments/staging
terraform init
terraform plan -out=tfplan
terraform apply tfplan          # 15 à 20 minutes
terraform output -raw app_url   # URL publique 
terraform destroy               # supprime
```

En local : copier `.env.example` en `.env`, renseigner `POSTGRES_PASSWORD`, puis `docker compose up --build`.

## Production, par priorité

1. **CI/CD** GitHub Actions avec OIDC : tests, scan, `plan` sur pull request, `apply` approuvé.
2. **État** distant verrouillé et environnement `production`.
3. **Sécurité** : PostgreSQL via Entra ID, Key Vault privé.
4. **Exploitation** : supervision, haute disponibilité, tests de restauration.
5. **Application** : authentification ; AKS seulement si nécessaire.
