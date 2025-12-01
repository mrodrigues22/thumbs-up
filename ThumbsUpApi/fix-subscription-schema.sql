-- Add missing columns to AspNetUsers if they don't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='AspNetUsers' AND column_name='SubscriptionId') THEN
        ALTER TABLE "AspNetUsers" ADD "SubscriptionId" uuid NULL;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='AspNetUsers' AND column_name='SubscriptionTier') THEN
        ALTER TABLE "AspNetUsers" ADD "SubscriptionTier" integer NOT NULL DEFAULT 0;
    END IF;
END $$;

-- Create Subscriptions table if it doesn't exist
CREATE TABLE IF NOT EXISTS "Subscriptions" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "UserId" text NOT NULL,
    "PaddleSubscriptionId" text NULL,
    "PaddleCustomerId" text NULL,
    "Status" integer NOT NULL,
    "Tier" integer NOT NULL,
    "PaddlePriceId" text NULL,
    "PlanName" text NULL,
    "CurrentPeriodStart" timestamp with time zone NULL,
    "CurrentPeriodEnd" timestamp with time zone NULL,
    "CancelledAt" timestamp with time zone NULL,
    "PausedAt" timestamp with time zone NULL,
    "TrialEndsAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "FK_Subscriptions_AspNetUsers_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- Create Transactions table if it doesn't exist
CREATE TABLE IF NOT EXISTS "Transactions" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "UserId" text NOT NULL,
    "SubscriptionId" uuid NULL,
    "PaddleTransactionId" text NOT NULL,
    "PaddleSubscriptionId" text NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Status" integer NOT NULL,
    "Type" integer NOT NULL,
    "Details" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "FK_Transactions_AspNetUsers_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Transactions_Subscriptions_SubscriptionId" 
        FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions" ("Id") ON DELETE SET NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_Subscriptions_PaddleSubscriptionId" ON "Subscriptions" ("PaddleSubscriptionId");
CREATE INDEX IF NOT EXISTS "IX_Subscriptions_UserId" ON "Subscriptions" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Transactions_PaddleTransactionId" ON "Transactions" ("PaddleTransactionId");
CREATE INDEX IF NOT EXISTS "IX_Transactions_SubscriptionId" ON "Transactions" ("SubscriptionId");
CREATE INDEX IF NOT EXISTS "IX_Transactions_UserId" ON "Transactions" ("UserId");

-- Update migration history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251201215956_AddSubscriptionTables', '9.0.11')
ON CONFLICT DO NOTHING;
