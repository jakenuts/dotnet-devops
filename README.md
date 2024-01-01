# dotnet-devops
 A dotnet global tool that lists projects and watches builds across an Azure Devops organization

 ![screenshot](https://raw.githubusercontent.com/jakenuts/dotnet-devops/main/docs/screenshot.png)

 ## Installation

 dotnet tool install --global dotnet-devops

 ## Setup

To setup dotnet-devops you'll need your Azure Devops organization url and a personal access token that allows read access to project pipelines & builds.

`devops init`

Example Url: 'https://mycompany.visualstudio.com/defaultcollection'

 ## Usage

- `devops init`    - sets the DevOps url and PAT to use [^1]
- `devops list`    - lists all builds
- `devops list -r` - lists recent builds
- `devops list -f` - lists failed builds
- `devops watch`   - polls for new builds and shows progress

 [^1]: The PAT is stored unencrypted in the .dotnet-devops folder created in your user profile directory. No idea how to do this better




