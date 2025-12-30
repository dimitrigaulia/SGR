-- Script de inicialização dos bancos de dados
-- Este script é executado automaticamente quando o container PostgreSQL é criado pela primeira vez

-- Criar banco de dados de configuração (backoffice)
CREATE DATABASE sgr_config;

-- Criar banco de dados de tenants
CREATE DATABASE sgr_tenants;

-- Conectar ao banco sgr_config e criar extensões necessárias
\c sgr_config;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Conectar ao banco sgr_tenants e criar extensões necessárias
\c sgr_tenants;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";


