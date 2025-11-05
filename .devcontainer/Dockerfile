FROM mcr.microsoft.com/vscode/devcontainers/dotnet:dev-8.0-bookworm

# Copy custom first notice message.
COPY first-run-notice.txt /tmp/staging/
RUN sudo mv -f /tmp/staging/first-run-notice.txt /usr/local/etc/vscode-dev-containers/ \
    && sudo rm -rf /tmp/staging

# Install Node.js and npm
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash - \
    && sudo apt-get install -y nodejs

# Install Azure Functions Core Tools
# Preview with aarch64 support
RUN sudo npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Install MCP Inspector
RUN sudo npm install -g @modelcontextprotocol/inspector

# Install azurite to emulate Azure Storage
RUN sudo npm install -g azurite

# Install Azure Developer CLI
RUN curl -fsSL https://aka.ms/install-azd.sh | bash
