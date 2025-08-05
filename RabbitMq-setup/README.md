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
      - ./config/node1/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro

    command: rabbitmq-server

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
      - ./config/node2/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro

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

    volumes:
      - rabbit3-data:/var/lib/rabbitmq
      - ./config/node3/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro

    depends_on:
      - rabbit2

  haproxy:
    image: haproxy:latest
    container_name: haproxy
    ports:
      - "5672:5672" # AMQP clients connect here
      - "15672:15672" # Web UI access via HAProxy
      - "8404:8404" # Optional: HAProxy stats UI
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

## `Check each node cookies`

```bash

docker exec rabbit1 cat /var/lib/rabbitmq/.erlang.cookie
docker exec rabbit2 cat /var/lib/rabbitmq/.erlang.cookie
docker exec rabbit3 cat /var/lib/rabbitmq/.erlang.cookie

```

---

## If all cookies are not same then replicate cookies across nodes

## 🔧 `replicate-cookies.sh`

```bash
#!/bin/bash

NODES=("rabbit1" "rabbit2" "rabbit3")

echo "🔍 Reading cookie from primary node (${NODES[0]})..."
REFERENCE_COOKIE=$(docker exec "${NODES[0]}" cat /var/lib/rabbitmq/.erlang.cookie 2>/dev/null)

if [ -z "$REFERENCE_COOKIE" ]; then
  echo "❌ Failed to read cookie from ${NODES[0]}"
  exit 1
fi

echo "📦 Cookie on ${NODES[0]}: $REFERENCE_COOKIE"
echo

for NODE in "${NODES[@]:1}"; do
  echo "🔍 Checking node: $NODE"
  NODE_COOKIE=$(docker exec "$NODE" cat /var/lib/rabbitmq/.erlang.cookie 2>/dev/null)

  echo "📦 Cookie on $NODE: $NODE_COOKIE"

  if [ "$NODE_COOKIE" != "$REFERENCE_COOKIE" ]; then
    echo "❗ Cookie mismatch on $NODE. Replacing with reference from ${NODES[0]}..."

    docker exec "$NODE" bash -c "echo '$REFERENCE_COOKIE' > /var/lib/rabbitmq/.erlang.cookie && \
      chmod 400 /var/lib/rabbitmq/.erlang.cookie && \
      chown rabbitmq:rabbitmq /var/lib/rabbitmq/.erlang.cookie"

    echo "✅ Cookie updated on $NODE"
  else
    echo "✅ Cookie matches on $NODE"
  fi

  echo
done

echo "🎯 Cookie sync complete."

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
