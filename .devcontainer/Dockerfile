FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

# # Copy custom first notice message.
# COPY first-run-notice.txt /tmp/staging/
# RUN sudo mv -f /tmp/staging/first-run-notice.txt /usr/local/etc/vscode-dev-containers/ \
#     && sudo rm -rf /tmp/staging

# # Install PowerShell 7.x
# RUN sudo apt-get update \
#     && sudo apt-get install -y wget apt-transport-https software-properties-common \
#     && wget -q https://packages.microsoft.com/config/ubuntu/$(. /etc/os-release && echo $VERSION_ID)/packages-microsoft-prod.deb \
#     && sudo dpkg -i packages-microsoft-prod.deb \
#     && sudo apt-get update \
#     && sudo apt-get install -y powershell

# Install Node.js and npm
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash - \
    && sudo apt-get install -y nodejs

# Install Azure Functions Core Tools
# Preview with aarch64 support
RUN sudo npm install -g azure-functions-core-tools@4.0.7332-preview1

# RUN curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg \
#     && sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg \
#     && sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list' \
#     && sudo apt-get update \
#     && sudo apt-get install -y azure-functions-core-tools-4

# Install Azure Developer CLI
RUN curl -fsSL https://aka.ms/install-azd.sh | bash

# # Install mechanical-markdown for quickstart validations
# RUN pip install mechanical-markdown
