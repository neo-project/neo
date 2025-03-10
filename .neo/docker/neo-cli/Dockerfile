FROM debian:stable-slim

LABEL org.opencontainers.image.description "neo-cli -- full node, no plugins"

# Install the apt-get packages
RUN apt-get update
RUN apt-get install -y libicu-dev libleveldb-dev screen sqlite3

COPY ./dist /opt/neo-cli
RUN ln -s /opt/neo-cli/neo-cli /usr/bin

CMD ["neo-cli"]
