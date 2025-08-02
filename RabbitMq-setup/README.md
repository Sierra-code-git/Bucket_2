# 🐇 RabbitMQ 3-Node Cluster with HAProxy (Manual Clustering)

## 📁 Directory Structure

```
your-project/
├── docker-compose.yml
├── haproxy.cfg
├── cluster-join.sh
```

---

## 🧱 `docker-compose.yml`

```yaml
version: "3.9"

services:
  rabbit1:
    image: rabbitmq:4.1.1-management
    container_name: rabbit1
    hostname: rabbit1
    networks:
      - rabbitmq-net
    ports:
      - "5675:5672"
      - "15675:15672"
    environment:
      - RABBITMQ_NODENAME=rabbit1
    volumes:
      - rabbit1-data:/var/lib/rabbitmq
    command: >
      bash -c "
        mkdir -p /var/lib/rabbitmq &&
        echo 'MYSECRETCOOKIEVALUE123' > /var/lib/rabbitmq/.erlang.cookie &&
        chmod 400 /var/lib/rabbitmq/.erlang.cookie &&
        chown rabbitmq:rabbitmq /var/lib/rabbitmq/.erlang.cookie &&
        rabbitmq-server
      "

  rabbit2:
    image: rabbitmq:4.1.1-management
    container_name: rabbit2
    hostname: rabbit2
    networks:
      - rabbitmq-net
    ports:
      - "5673:5672"
      - "15673:15672"
    environment:
      - RABBITMQ_NODENAME=rabbit2
    volumes:
      - rabbit2-data:/var/lib/rabbitmq
    command: >
      bash -c "
        mkdir -p /var/lib/rabbitmq &&
        echo 'MYSECRETCOOKIEVALUE123' > /var/lib/rabbitmq/.erlang.cookie &&
        chmod 400 /var/lib/rabbitmq/.erlang.cookie &&
        chown rabbitmq:rabbitmq /var/lib/rabbitmq/.erlang.cookie &&
        rabbitmq-server
      "
    depends_on:
      - rabbit1

  rabbit3:
    image: rabbitmq:4.1.1-management
    container_name: rabbit3
    hostname: rabbit3
    networks:
      - rabbitmq-net
    ports:
      - "5674:5672"
      - "15674:15672"
    environment:
      - RABBITMQ_NODENAME=rabbit3
      - RABBITMQ_ERLANG_COOKIE=MYSECRETCOOKIEVALUE123
    volumes:
      - rabbit3-data:/var/lib/rabbitmq
    command: >
      bash -c "
        mkdir -p /var/lib/rabbitmq &&
        echo 'MYSECRETCOOKIEVALUE123' > /var/lib/rabbitmq/.erlang.cookie &&
        chmod 400 /var/lib/rabbitmq/.erlang.cookie &&
        chown rabbitmq:rabbitmq /var/lib/rabbitmq/.erlang.cookie &&
        rabbitmq-server
      "
    depends_on:
      - rabbit2

  haproxy:
    image: haproxy:latest
    container_name: haproxy
    ports:
      - "5672:5672"
      - "15672:15672"
      - "8404:8404"
    volumes:
      - ./haproxy.cfg:/usr/local/etc/haproxy/haproxy.cfg:ro
    networks:
      - rabbitmq-net
    depends_on:
      - rabbit1
      - rabbit2
      - rabbit3

networks:
  rabbitmq-net:
    driver: bridge

volumes:
  rabbit1-data:
  rabbit2-data:
  rabbit3-data:
```

---

## ⚙️ `haproxy.cfg`

```cfg
global
  log stdout format raw local0

defaults
  log global
  option tcplog
  timeout connect 5s
  timeout client  60s
  timeout server  60s

frontend amqp
  bind *:5672
  default_backend rabbitmq_amqp

frontend rabbitmq_ui
  bind *:15672
  default_backend rabbitmq_ui_back

listen stats
  bind *:8404
  mode http
  stats enable
  stats uri /
  stats refresh 10s

backend rabbitmq_amqp
  balance roundrobin
  option tcp-check
  server rabbit1 rabbit1:5672 check
  server rabbit2 rabbit2:5672 check
  server rabbit3 rabbit3:5672 check

backend rabbitmq_ui_back
  balance roundrobin
  option httpchk GET /
  server rabbit1 rabbit1:15672 check
  server rabbit2 rabbit2:15672 check
  server rabbit3 rabbit3:15672 check
```

---

## 🔧 `cluster-join.sh`

```bash
#!/bin/bash

echo "[*] Joining rabbit2 to cluster..."
docker exec rabbit2 rabbitmqctl stop_app
docker exec rabbit2 rabbitmqctl reset
docker exec rabbit2 rabbitmqctl join_cluster rabbit1@rabbit1
docker exec rabbit2 rabbitmqctl start_app

echo "[*] Joining rabbit3 to cluster..."
docker exec rabbit3 rabbitmqctl stop_app
docker exec rabbit3 rabbitmqctl reset
docker exec rabbit3 rabbitmqctl join_cluster rabbit1@rabbit1
docker exec rabbit3 rabbitmqctl start_app

echo "[*] Checking cluster status from rabbit1..."
docker exec rabbit1 rabbitmqctl cluster_status
```

---

## 🚀 How to Use

1. Create the files: `docker-compose.yml`, `haproxy.cfg`, `cluster-join.sh`
2. Start the containers:

```bash
docker-compose up -d
```

3. Wait ~20 seconds, then run:

```bash
./cluster-join.sh
```

4. Access Web UIs:
   - RabbitMQ UI: http://localhost:15672 (user/pass: `guest`/`guest`)
   - HAProxy Stats: http://localhost:8404

---

## ✅ Done!
- Manual clustering using consistent Erlang cookie per container
- Volumes ensure persistence
- HAProxy balances across all nodes for AMQP + Web UI