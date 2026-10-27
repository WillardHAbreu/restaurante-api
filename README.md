# 🍽️ Restaurante API

API RESTful para sistema de reserva de mesas em restaurantes, desenvolvida em ASP.NET Core 10 e PostgreSQL.

## 🚀 API em Produção
* **Swagger UI / Documentação:** https://restaurante-api-dftz.onrender.com/index.html
* **Base de Dados:** PostgreSQL (Neon)
* **Hospedagem:** Render

## 🛠️ Tecnologias Utilizadas
* C# 14 / .NET 10
* ASP.NET Core Web API
* Entity Framework Core 10
* PostgreSQL (Neon)
* JWT Bearer Authentication
* Swagger / Swashbuckle
* Docker

## 📌 Endpoints Principais
* `POST /api/auth/registrar` - Cadastro de novos usuários
* `POST /api/auth/login` - Autenticação e geração de Token JWT
* `GET /api/mesas` - Listagem de mesas disponíveisS