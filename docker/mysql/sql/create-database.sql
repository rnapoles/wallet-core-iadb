-- Crear usuario 'wallet-system'
CREATE USER 'wallet-system'@'%' IDENTIFIED BY 'auth-p@55w0rd';

-- Crear base de datos y asignar privilegios
CREATE DATABASE IF NOT EXISTS `wallet-system`;
GRANT ALL PRIVILEGES ON `wallet-system`.* TO 'wallet-system'@'%';

