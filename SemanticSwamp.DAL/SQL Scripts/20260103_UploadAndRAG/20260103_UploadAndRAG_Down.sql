USE SemanticSwamp
GO

/****** Object:  Table [dbo].[DocumentUploadTerms]    Script Date: 1/3/2026 3:07:28 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentUploadTerms]') AND type in (N'U'))
DROP TABLE [dbo].[DocumentUploadTerms]
GO

/****** Object:  Table [dbo].[DocumentUploads]    Script Date: 12/18/2025 11:14:26 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentUploads]') AND type in (N'U'))
DROP TABLE [dbo].[DocumentUploads]
GO


/****** Object:  Table [dbo].[Terms]    Script Date: 1/3/2026 3:08:21 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Terms]') AND type in (N'U'))
DROP TABLE [dbo].[Terms]
GO



/****** Object:  Table [dbo].[Collections]    Script Date: 1/3/2026 3:07:48 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Collections]') AND type in (N'U'))
DROP TABLE [dbo].[Collections]
GO

/****** Object:  Table [dbo].[Categories]    Script Date: 1/3/2026 3:08:08 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
DROP TABLE [dbo].[Categories]
GO





