CREATE TABLE [dbo].[X5_wsComment] (
    [AutoCode]     INT            IDENTITY (1, 1) NOT NULL,
    [lNewsCode]    INT            NULL,
    [sContent]     NVARCHAR (200) NULL,
    [dCreateTime]  DATETIME       NULL,
    [sPersoncode]  NVARCHAR (20)  NULL,
    [lReplyToCode] INT            NULL,
    [lState]       INT            NULL,
    [lZan]         INT            NULL
);

