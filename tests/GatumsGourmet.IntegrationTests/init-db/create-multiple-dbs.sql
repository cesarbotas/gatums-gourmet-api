-- Script de inicialização do PostgreSQL para os testes de integração.
-- O POSTGRES_DB já cria o banco 'gatums' (produção/dev local). Aqui criamos
-- adicionalmente o 'gatums_test', usado pelos testes de integração, com o
-- mesmo usuário/senha definidos no docker-compose.
--
-- Executado automaticamente pelo entrypoint do Postgres (diretório
-- /docker-entrypoint-initdb.d) apenas na primeira inicialização do volume.

SELECT 'CREATE DATABASE gatums_test OWNER gatums'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'gatums_test')\gexec
