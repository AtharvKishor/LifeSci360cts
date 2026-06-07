-- Create login if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'lifesci_app')
BEGIN
    CREATE LOGIN lifesci_app WITH PASSWORD = 'LifeSci@App2024!';
END

-- Create databases if they don't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LifeSci360_Services')
    CREATE DATABASE LifeSci360_Services;

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LifeSci360_Audit')
    CREATE DATABASE LifeSci360_Audit;

-- Grant db_owner on LifeSci360_Services
USE LifeSci360_Services;
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'lifesci_app')
BEGIN
    CREATE USER lifesci_app FOR LOGIN lifesci_app;
    ALTER ROLE db_owner ADD MEMBER lifesci_app;
END

-- Grant db_owner on LifeSci360_Audit
USE LifeSci360_Audit;
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'lifesci_app')
BEGIN
    CREATE USER lifesci_app FOR LOGIN lifesci_app;
    ALTER ROLE db_owner ADD MEMBER lifesci_app;
END
