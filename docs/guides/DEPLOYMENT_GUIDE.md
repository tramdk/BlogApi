# 🚀 Deployment Guide: Hosted for Free on Render.com

This guide will walk you through deploying your **BlogApi** to the internet using **Render.com**, a modern cloud provider with excellent free tier support for Docker and PostgreSQL.

---

## 📋 Prerequisites
1. A [GitHub Account](https://github.com/).
2. A [Render.com Account](https://render.com/) (Sign up with GitHub).
3. Your code pushed to a GitHub repository.

---

## 🗄️ Step 1: Create a Hosted Database (PostgreSQL)
Since SQL Server is rarely free, we will use **PostgreSQL**, which our code now supports natively.

1. Go to your [Render Dashboard](https://dashboard.render.com/).
2. Click **New +** -> **PostgreSQL**.
3. **Name**: `blog-db` (or anything you like).
4. **Region**: `Singapore (Southeast Asia)` (Lowest latency for Vietnam).
5. **Instance Type**: Select **Free**.
6. Click **Create Database**.
7. ⏳ Wait a moment for it to be created.
8. **IMPORTANT**: Find the **Internal Connection String**. It looks like:
   `postgres://blog_db_user:password@hostname:5432/blog_db`
   👉 **Copy this string.** You will need it in Step 2.

---

## 🐳 Step 2: Deploy the Web API (Docker)
1. Go back to Dashboard -> **New +** -> **Web Service**.
2. Select **Build and deploy from a Git repository**.
3. Connect your **BlogApi** repository.
4. **Name**: `blog-api`.
5. **Region**: `Singapore`.
6. **Branch**: `main`.
7. **Runtime**: Select **Docker**. (Render will automatically find our `Dockerfile`).
8. **Instance Type**: Select **Free**.
9. Scroll down to **Environment Variables**. Click **Add Environment Variable** for each line below:

| Key | Value | Description |
|-----|-------|-------------|
| `DatabaseProvider` | `PostgreSQL` | Switch app to use Npgsql provider. |
| `ConnectionStrings__DefaultConnection` | `[PASTE_YOUR_CONNECTION_STRING_HERE]` | Paste the **Internal Connection String** from Step 1. |
| `Jwt__Secret` | `some-super-secret-key-at-least-32-chars-long` | Random string for token security. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Tells .NET to run in Prod mode. |

10. Click **Create Web Service**.

---

## 🌍 Step 3: Verify Deployment
1. Render will start building your Docker image. This might take **5-8 minutes** for the first time.
2. Watch the logs. When you see `Build successful` and `Application started`, it's live!
3. Look for the URL at the top left (e.g., `https://blog-api-xyz.onrender.com`).
4. Test it by adding `/scalar/v1` to the end of the URL:
   `https://blog-api-xyz.onrender.com/scalar/v1`

**🎉 Congratulations! Your Enterprise API is now live on the internet!**

---

## 🔍 Troubleshooting

**Q: My build failed?**
A: Check the logs. Did you include the `.dockerignore` file? If `test_uploads` folder was not ignored, the build context might be too large.

**Q: "Database does not exist"?**
A: Render usually creates the default database for you. But if you customized it, ensure the connection string matches. Our app automatically applies Migrations on startup (`app.SeedDatabaseAsync()` in `Program.cs`), so tables will be created automatically.

**Q: Scalar UI is not loading?**
A: Ensure you are accessing `/scalar/v1`. If you get a 404, check if you set `ASPNETCORE_ENVIRONMENT` to `Production` correctly. (Our code enables Swagger/Scalar even in non-dev if configured, but sometimes middleware order matters).
*Note: In our current code, Scalar/Swagger is inside `if (app.Environment.IsDevelopment())`. If you strictly want it in Production, you need to remove that check in `Program.cs` or set env variable to `Development` (not recommended for true prod, but okay for hobby).*

**Tip**: To enable Docs in Production safely, go to `Program.cs` and move `app.UseSwagger(...)` outside the `if (IsDevelopment)` block.
