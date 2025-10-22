
# Purpose

I am use this application to run remote AI instances on Lambda labs. 
This code uses a number of technologies including GitHub client, 
SSH to allow the remote deployment of code to Lambda labs instances.

## Setup 

1. Clone the repository to your local machine.
2. Set an environment variable named SECRET_PATH to point to the directory where your
   lambda labs key and github token are stored.
3. The lambda labs key should be named `lambda_cloude_api.txt` and the github token
   should be named `git_secret.txt`.
4. Build and run the application using your preferred IDE or .NET CLI.

### Build Commands
Assuming you have .NET SDK installed, you can build the application using the following commands:
```angular2html
dotnet retore
dotnet build .
```

## Functionality

1. Allows you to launch and delete Lambda labs instances.
2. Deploys a website that lists the GPU utilization of the instance.
3. Uses SSH to connect to the instance and run commands remotely.
4. Uses GitHub API to clone repositories and manage code deployment.
5. Allows you to upload files to the remote instance.

## Architecture

Below is a high-level architecture diagram of the application:
![Architecture](https://github.com/cschrumm/AvaloniaLambdaLab/blob/master/Service.Library/Docs/DiagramView.png)

## Notes

Please note that this application is without warranty and is intended for educational purposes only.
Use it at your own risk. Make sure to review and understand the code before using it in a production environment.
Always follow best practices for security, especially when dealing with API keys and sensitive data.

This application is still work in progress and may have bugs or incomplete features.

![screen shot](https://github.com/cschrumm/AvaloniaLambdaLab/blob/master/Service.Library/Docs/LatestScreen.png)





