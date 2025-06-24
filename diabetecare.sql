--SQL Codes--

--SignUp SQL--

CREATE TABLE Users (
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY ,
    role VARCHAR(300) NOT NULL ,
    firstName VARCHAR(100) NOT NULL,
    lastName VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    passwordHash VARCHAR(255) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    specialization VARCHAR(255),
    hospital VARCHAR(255),
    licenseNumber VARCHAR(255),
    patientId INT,
    relationship VARCHAR(100),
    createdAt DATETIME DEFAULT CURRENT_TIMESTAMP
);


--Health Logging SQL -- 
