/*
  Run once against the master database (e.g. in SSMS or with sqlcmd) before schema.sql
  if you are not letting the API create the database for you.

    sqlcmd -S localhost -U sa -i create-database.sql
    sqlcmd -S localhost -U sa -d Changelog -i schema.sql
*/
IF DB_ID(N'Changelog') IS NULL
    CREATE DATABASE Changelog;
GO
