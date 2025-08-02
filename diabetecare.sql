--SQL Codes--

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


INSERT INTO Users (
    role, firstName, lastName, email, passwordHash, phone,
    specialization, hospital
)
VALUES (
    'dietician', 'Emily', 'Smith', 'emily@gmail.com', '12345',
    '+15551234567', 'Sports Nutrition', 'Wellness Center'
);

SELECT * FROM Users
DROP TABLE Users


CREATE TABLE HealthReadings (
    Id VARCHAR(50) PRIMARY KEY,
    UserId INT NOT NULL,
    Date DATE NOT NULL,
    Time TIME NOT NULL,
    Timestamp DATETIME NOT NULL,
    BloodSugar FLOAT NOT NULL,
    InsulinDosage FLOAT NOT NULL,
    BodyWeight FLOAT NULL,
    SystolicBP INT NULL,
    DiastolicBP INT NULL,
    HeartRate INT NULL,
    MealContext VARCHAR(500),
    Notes TEXT,
	Status VARCHAR(800),

    CONSTRAINT FK_HealthReadings_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id)
        ON DELETE CASCADE
)

Select * FROM HealthReadings
Drop Table HealthReadings


CREATE TABLE ProviderAvailability (
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    providerId INT NOT NULL,  -- FK to Users
    availabilityDate DATE NOT NULL,
    startTime TIME NOT NULL,
    endTime TIME NOT NULL,
    notes VARCHAR(500),
    createdAt DATETIME DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_ProviderAvailability_Users FOREIGN KEY (providerId) REFERENCES Users(id)
);

Select * From ProviderAvailability;

ALTER TABLE ProviderAvailability
ADD status VARCHAR(200) NOT NULL DEFAULT 'available';


CREATE TABLE WordsOfEncouragement (
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    patientId INT NOT NULL,
    familyId INT NOT NULL,
    messageDate DATE NOT NULL,
    messageTime TIME NOT NULL,
    content TEXT NOT NULL,
    createdAt DATETIME DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Encouragement_Patient FOREIGN KEY (patientId) REFERENCES Users(id),
    CONSTRAINT FK_Encouragement_Family FOREIGN KEY (familyId) REFERENCES Users(id)
);

SELECT * FROM WordsOfEncouragement;

SELECT * FROM FollowUps;


CREATE TABLE MealEntries (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    EntryDate DATE NOT NULL,
    MealType NVARCHAR(20) NOT NULL CHECK (MealType IN ('breakfast', 'lunch', 'dinner')),
    FoodItem NVARCHAR(255) NOT NULL,
    Portion NVARCHAR(100) NOT NULL,
    Notes NVARCHAR(MAX),
    CONSTRAINT FK_MealEntries_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id)
        ON DELETE CASCADE
);

SELECT * FROM MealEntries;

DELETE FROM MealEntries;

CREATE TABLE MedicationReminders (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    MedicationName VARCHAR(700) NOT NULL,
    Description NVARCHAR(MAX),
    Dosage VARCHAR(50),
    ReminderDate DATE NOT NULL,          -- New DATE column
    ReminderDue TIME NOT NULL,
    Notes NVARCHAR(MAX),
    Status VARCHAR(20) DEFAULT 'pending' CHECK (Status IN ('pending', 'taken')),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_MedicationReminders_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id)
        ON DELETE CASCADE
);


SELECT * FROM MedicationReminders;
DROP TABLE MedicationReminders;

DELETE from MedicationReminders;

INSERT INTO ProviderAvailability (
    providerId,
    availabilityDate,
    startTime,
    endTime,
    notes,
    status
)
VALUES (
    7,                                -- providerId
    '2025-07-01',                     -- availabilityDate (YYYY-MM-DD)
    '10:00:00',                       -- startTime (HH:MM:SS)
    '11:00:00',                       -- endTime (HH:MM:SS)
    'Morning appointment slots',      -- notes
    'Available'                       -- status
);

Select * From ProviderAvailability

CREATE TABLE PatientAppointmentBooking (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PatientID INT NOT NULL,
    ProviderID INT NOT NULL,
    Role NVARCHAR(100) NOT NULL, -- new Role column
    ProviderName NVARCHAR(500) NOT NULL,
    ProviderSpecialization NVARCHAR(500) NOT NULL,
    ProviderVenue NVARCHAR(800) NOT NULL,
    ProviderAvailableDate DATE NOT NULL,
    ProviderAvailableTimeSlot NVARCHAR(50) NOT NULL,
    BookingMode NVARCHAR(50) NOT NULL,
    ServiceBooked NVARCHAR(100) NOT NULL,
    ReasonsForVisit NVARCHAR(500) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'booking',
    CONSTRAINT FK_Patient FOREIGN KEY (PatientID) REFERENCES Users(id),
    CONSTRAINT FK_Provider FOREIGN KEY (ProviderID) REFERENCES Users(id)
);

SELECT * FROM PatientAppointmentBooking
DROP TABLE PatientAppointmentBooking

SELECT name
FROM sys.tables
ORDER BY name;

SELECT * FROM PatientMedicalInfo
SELECT * FROM Prescriptions
SELECT * FROM Medications 
SELECT * FROM TreatmentPlans 
SELECT * FROM PhysicianNotifications


CREATE TABLE DietTip (
        Id INT IDENTITY(1,1) PRIMARY KEY,
    DieticianId INT NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_MealTips_Physician
        FOREIGN KEY (DieticianId) REFERENCES Users(id)
);

SELECT * FROM DietTip
DROP TABLE DietTip

CREATE TABLE DietPlan (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DieticianID INT NOT NULL,
    PatientID INT NOT NULL,
    MealType NVARCHAR(100) NOT NULL,
    MealPlan NVARCHAR(MAX) NOT NULL,
    CreatedDate DATE NOT NULL,  -- NO DEFAULT, must be provided
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_MealPlan_Dietician FOREIGN KEY (DieticianID) REFERENCES Users(id),
    CONSTRAINT FK_MealPlan_Patient FOREIGN KEY (PatientID) REFERENCES Users(id)
);

DROP TABLE DietPlan
Select * FROM DietPlan


CREATE TABLE MedicalSupply (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FamilyId INT NOT NULL,
    PatientId INT NOT NULL,
    MedicineName NVARCHAR(255) NOT NULL,
    MedicineDescription NVARCHAR(1000),
    Quantity INT NOT NULL,
	Unit NVARCHAR(50),
    PlaceToPurchase NVARCHAR(500),
    ExpirationDate DATE,
    Status NVARCHAR(100),
    Notes NVARCHAR(2000),
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_MedicalSupply_FamilyId FOREIGN KEY (FamilyId) REFERENCES Users(Id),
    CONSTRAINT FK_MedicalSupply_PatientId FOREIGN KEY (PatientId) REFERENCES Users(Id)
);

SELECT * FROM MedicalSupply
SELECT * FROM Users


DROP TABLE MedicalSupply

