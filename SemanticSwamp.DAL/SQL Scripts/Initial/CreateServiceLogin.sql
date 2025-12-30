USE [master]
GO

/* For security reasons the login is created disabled and with a random password. */
/****** Object:  Login [semanticSwampServiceLogin]    Script Date: 12/30/2025 2:00:00 PM ******/
CREATE LOGIN [semanticSwampServiceLogin] WITH PASSWORD=N'wpxnmBPokZq8UXkxmS3AqmaX/Ivp9ac3FS0vfmu1j1U=', DEFAULT_DATABASE=[SemanticSwamp], DEFAULT_LANGUAGE=[us_english], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF
GO

ALTER LOGIN [semanticSwampServiceLogin] DISABLE
GO

ALTER SERVER ROLE [sysadmin] ADD MEMBER [semanticSwampServiceLogin]
GO


