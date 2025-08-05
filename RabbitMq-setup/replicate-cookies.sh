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
