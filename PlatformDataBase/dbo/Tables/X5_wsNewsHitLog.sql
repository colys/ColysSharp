CREATE TABLE [dbo].[X5_wsNewsHitLog] (
    [AutoCode]    INT              IDENTITY (1, 1) NOT NULL,
    [dAddTime]    DATETIME         NULL,
    [sPersonCode] NVARCHAR (50)    NULL,
    [sIP]         NVARCHAR (200)   NULL,
    [sUrl]        NVARCHAR (500)   NULL,
    [lNewsCode]   INT              NULL,
    [sUserAgent]  NVARCHAR (500)   NULL,
    [sGuid]       UNIQUEIDENTIFIER NULL
);

