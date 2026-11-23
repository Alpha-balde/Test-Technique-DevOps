# Demandes de déploiement : test technique DevOps

Application .NET 10 + Angular déployée de bout en bout sur Azure par un seul `terraform apply`.
Une demande associe une **application**, un **environnement** (`staging` ou `production`) et une **version**.
Une seule demande est acceptée par triplet ; un doublon est refusé en **HTTP 409** avec un message affiché à l'écran.

## 1. Architecture

```
Navigateur ──HTTPS──▶ Azure Container Apps : image unique (API ASP.NET Core 10 + écran Angular)
                        ├─ identité managée ──▶ Key Vault (mot de passe, chaîne de connexion)
                        └─ VNet privé ──5432──▶ PostgreSQL Flexible Server 17 (accès public désactivé, DNS privé)
```

Dépôt : `app/` (API, tests, `web/` Angular, `Dockerfile`), `modules/container-app-postgres/` (module réutilisable),
`environments/staging/` (appel du module et variables, sans secret).

## 2. Démarche

1. **API d'abord** : modèle, migration EF Core versionnée, règle d'unicité garantie en base, tests d'intégration sur un vrai PostgreSQL (Testcontainers).
2. **Écran Angular unique**, servi par l'API dans **une seule image Docker** : même origine, pas de CORS, un seul artefact.
3. **Image publiée sur GHCR** (publique) et **épinglée par digest** dans Terraform.
4. **Module Terraform** et environnement `staging` : `fmt`, `validate`, tests du module (providers simulés), relecture de chaque plan sauvegardé avant `apply`.
5. **Vérification en ligne** : `/health`, création d'une demande, doublon refusé en 409.

## 3. Choix techniques et compromis

- **Container Apps** : HTTPS géré, mise à l'échelle, sondes. Démarrage et vivacité sur `/health/live` (sans dépendance, donc pas de boucle de redémarrage si la base tombe) ; disponibilité sur `/health` (inclut la base).
- **PostgreSQL privé** : sous-réseau délégué, NSG n'ouvrant le port 5432 qu'au sous-réseau applicatif, zone DNS privée.
- **Secrets** : le mot de passe est généré pendant l'`apply` par une ressource Terraform éphémère, écrit dans Key Vault, puis relu depuis Key Vault pour configurer PostgreSQL et la chaîne de connexion. Ces valeurs ne passent que par des arguments *write-only* : **aucun secret dans le dépôt, le plan ou l'état Terraform**. Container Apps résout la référence Key Vault avec l'identité managée de l'application (rôle *Key Vault Secrets User*) et injecte la chaîne de connexion en variable d'environnement ; le code n'utilise aucun SDK Key Vault.
- **Règle métier** : index unique sur (application normalisée, environnement, version) ; la violation est traduite en 409, y compris pour des requêtes simultanées.
- **Migrations automatiques** au démarrage, avec tentatives bornées ; la sonde de démarrage est calibrée sur leur durée maximale.
- **Compromis acceptés pour le test** :
  - état Terraform **local** (il ne contient aucun secret) ; en production : backend `azurerm` distant avec verrouillage ;
  - l'application utilise le compte administrateur PostgreSQL ; Key Vault reste joignable publiquement mais protégé par Entra ID et RBAC ;
  - attente fixe de 300 s pour la propagation des rôles RBAC avant l'écriture des secrets ;
  - **région North Europe** : France Central et West Europe refusaient PostgreSQL Flexible Server pour cet abonnement ; North Europe a été validée par un test de capacité ;
  - le **quota actuel de cet abonnement** limite à un environnement Container Apps : un second environnement (production) demanderait une hausse de quota ou un autre abonnement. Ce n'est pas une limite du module.

## 4. Rejouer le déploiement

Prérequis : Terraform ≥ 1.11, Azure CLI, droits *Owner* sur l'abonnement (le module crée des attributions de rôle).

```powershell
az login
cd environments/staging
terraform init
terraform plan -out=tfplan
terraform apply tfplan          # environ 15 à 20 minutes
terraform output -raw app_url   # URL publique ; ajouter /health pour le contrôle de santé
terraform destroy               # supprime tout, Key Vault purgé
```

Note de nettoyage : à la création du VNet, Azure crée automatiquement `NetworkWatcherRG` (Network Watcher régional).
Cette ressource partagée et gratuite n'est pas gérée par Terraform et reste en place après `destroy`.

En local : `docker compose up --build` (copier `.env.example` en `.env`).
Tests : `dotnet test app/DeployRequests.slnx`, `npm test` dans `app/web`, `terraform test` dans le module.

## 5. Pour une production réelle, par priorité

1. **CI/CD** (GitHub Actions) : build, tests et scan de l'image, publication par digest, `plan` sur pull request, `apply` après approbation, authentification Azure par fédération OIDC (sans secret).
2. **État et environnements** : backend distant verrouillé, environnement `production` (quota Container Apps ou abonnement dédié).
3. **Sécurité** : authentification PostgreSQL par Entra ID avec l'identité managée, endpoint privé pour Key Vault, signature des images.
4. **Supervision** : logs centralisés, métriques et alertes (Azure Monitor, ou Prometheus et Grafana).
5. **Fiabilité** : haute disponibilité PostgreSQL, tests de restauration, déploiements blue-green ou canary via les révisions Container Apps.
6. **Application** : authentification des utilisateurs (Entra ID), pagination de la liste. Kubernetes (AKS + Helm) seulement si un contrôle plus fin devient nécessaire.
