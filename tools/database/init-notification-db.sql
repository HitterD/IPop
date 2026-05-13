-- Run after SQL container up, before EF migrations
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'IPop')
BEGIN
    CREATE DATABASE IPop;
END
GO
