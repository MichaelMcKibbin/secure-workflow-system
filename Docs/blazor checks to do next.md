
Advice from hostinger:
Compare local Dockerfile (the one that produces the image that works on Docker Desktop) with what’s actually pushed to GHCR at ghcr.io/michaelmckibbin/secure-workflow-system. Make sure the publish step is the same and that you’re not:
Using a different publish profile (e.g. Development vs Production) that alters static file output.
Using a runtime-only image layer that copies only the DLLs and forgets the wwwroot/Blazor output.
Check your Program.cs / Startup for Production vs Development differences:
Ensure in Production you still call UseStaticFiles() and, for WASM, UseBlazorFrameworkFiles() (if it’s a hosted WASM app).
From your own machine, run inside the container image you built for GHCR:
docker run --rm -it ghcr.io/michaelmckibbin/secure-workflow-system@sha256:67c1dbb0bb5e661ee12e083d7379204611bafeb7aee4c5848f12ef15c864bf72 \
  bash -c &quot;find /app -maxdepth 5 -name &apos;blazor.web.js&apos; -o -path &apos;*_framework*&apos;&quot;
If that returns nothing, the image being pushed to GHCR simply doesn’t contain the file, and you’ll need to adjust your Dockerfile/publish step.


That makes sense. If the Dockerfile is identical, the likely difference is the build/push/deploy process, not the file itself.

On the VPS, the app is running and serving the page, so this looks more like a published image/content mismatch than a container runtime problem.

Check these 3 things in your deployment setup:

same build command
same publish output
same image tag/digest being deployed on VPS
If you want, send screenshots of the build and deploy settings and I’ll help compare them.

========
copilot on github - gpt 4.1
======
Summary of the Problem
Problem: blazor.web.js returns a 404 error when accessing the deployed app on your VPS (workflow-1 container).
Observed: Searching in the container finds no blazor.web.js file.
Localhost: The app runs fine locally.
Suspected Cause: There appears to be a discrepancy between local setup and deployment via GHCR (GitHub Container Registry) to your VPS.
Investigation Plan
Check how blazor.web.js is generated and copied in both local and container builds.
Compare build, publish, and Dockerfile steps for local and VPS deployment.
Collect info from your workflows, Dockerfile(s), and static files directory.
Propose root causes and solutions.
Let's proceed:

1. How is blazor.web.js Used/Generated?
For Blazor (especially Blazor WebAssembly and Blazor Server with Interactive WebAssembly), blazor.web.js is:

Usually found in the application's wwwroot folder when published.
Included as an embedded resource in the Microsoft.AspNetCore.Components.WebAssembly package.
Copied during the publish/build phase into wwwroot/_framework (or similar path).
If this file is missing in the deployment container but present locally, it suggests a difference in the publish or copy step.

2. Compare Local vs Container Build Steps
Localhost:

You probably run dotnet publish or build using Visual Studio/CLI, which places all static assets in the publish output (e.g., bin/Release/netX/wwwroot).
You run the app (dotnet run or via dev server), and wwwroot is served.
VPS + GHCR (Container):

Usually, the Dockerfile will COPY the published output into the image.
If the publish step or copy path is wrong, the static files (including _framework/blazor.web.js) may be omitted.
3. Specific Checks & File Search
a. Dockerfile Steps
Common .NET Blazor Dockerfile steps look like:

Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["YourApp/YourApp.csproj", "YourApp/"]
RUN dotnet restore "YourApp/YourApp.csproj"
COPY . .
WORKDIR "/src/YourApp"
RUN dotnet publish "YourApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YourApp.dll"]
Confirm that the dotnet publish output includes the full wwwroot directory with its _framework subfolder and blazor.web.js.
Confirm that COPY --from=build /app/publish . is correct and not missing static assets.
b. Static Files Middleware
In Blazor Server, the app must include .UseStaticFiles() in Startup.cs or Program.cs. If your VPS Docker build COPYs to a different folder (not matching what the application expects), static files won't be served.

c. Publish Directory Check
After a container build, you can check the contents of /app/publish/wwwroot/_framework inside the image to see if blazor.web.js exists.

4. Potential Root Causes
Mismatch in publish and Docker COPY step: The Dockerfile might not copy the correct folder where blazor.web.js is output.
Incorrect working directory in Dockerfile: The final built image isn't serving files from the correct path.
Build/publish artifacts not complete: Publish step in Docker might use a different configuration or be missing resources.
Static files not served: The app in the container might not serve static files correctly due to missing configuration.
Next Steps to Diagnose and Fix
Check Dockerfile steps.

Confirm that dotnet publish is being run (not just dotnet build).
Confirm that the publish output directory being copied matches what's being used in the container.
Check publish output before/after build and containerization:

Does wwwroot/_framework/blazor.web.js exist in your local bin/Release/netX/publish/wwwroot/_framework?
After build, does this file exist in the container?
You can do docker run --rm -it yourimage bash and inspect the directory contents.
Check application static files configuration so /_framework/blazor.web.js is served.

If using a separate static site (reverse proxy), ensure it’s routing static files correctly.

Troubleshooting Checklist
 Is the dotnet publish step run in the Dockerfile?
 Is wwwroot/_framework/blazor.web.js present in the publish output on the host and inside the built container?
 Is COPY --from=build /app/publish . (or equivalent) present in the Dockerfile?
 Is UseStaticFiles() called in Program.cs/Startup.cs?
 Is the container serving files from the expected path? (Check for any path changes.)
 Are there differences between local and containerized publish output?
Common Fixes
Always use dotnet publish (not dotnet build) in your Dockerfile.
Use the publish directory as the input for your COPY step.
Double-check the output/copy path in your Dockerfile matches what is expected by your application.
If You Share Your Dockerfile & Relevant Build/Deployment Workflow
I can analyze the actual files and give you step-by-step fixes for your config.

Summary:
Your issue is almost certainly due to a difference in the publish output being copied into your container, or static files not being served/configured properly. Check your Dockerfile publish/COPY steps and static files middleware setup. Share relevant snippets for a precise fix!