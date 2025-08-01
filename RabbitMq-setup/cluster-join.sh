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
