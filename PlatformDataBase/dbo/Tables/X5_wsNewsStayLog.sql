CREATE TABLE [dbo].[X5_wsNewsStayLog] (
    [AutoCode]    INT              IDENTITY (1, 1) NOT NULL,
    [dAddTime]    DATETIME         NULL,
    [sPersonCode] NVARCHAR (50)    NULL,
    [lNewsCode]   INT              NULL,
    [sGuid]       UNIQUEIDENTIFIER NULL
);

