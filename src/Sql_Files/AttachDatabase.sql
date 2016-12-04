select name from sys.databases


USE [master]
GO
CREATE DATABASE [DrpServerQueueExample00001] ON 
( FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL10.SQLEXPRESS\MSSQL\DATA\DrpServerQueueExample00001.mdf' ),
( FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL10.SQLEXPRESS\MSSQL\DATA\DrpServerQueueExample00001_log.ldf' )
 FOR ATTACH ;
GO
