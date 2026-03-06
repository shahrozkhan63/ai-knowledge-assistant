-- Create documents table
CREATE TABLE IF NOT EXISTS documents (
    id SERIAL PRIMARY KEY,
    "Title" TEXT NOT NULL DEFAULT '',
    content TEXT NOT NULL,
    embedding double precision[],
    "CreatedAt" TIMESTAMP DEFAULT NOW()
);

-- Create cosine similarity function
CREATE OR REPLACE FUNCTION cosine_similarity(
    a double precision[], 
    b double precision[]
)
RETURNS double precision AS $$
DECLARE
    dot float8 := 0;
    mag_a float8 := 0;
    mag_b float8 := 0;
    i int;
BEGIN
    FOR i IN 1..array_length(a,1) LOOP
        dot := dot + a[i] * b[i];
        mag_a := mag_a + a[i] * a[i];
        mag_b := mag_b + b[i] * b[i];
    END LOOP;
    IF mag_a = 0 OR mag_b = 0 THEN
        RETURN 0;
    END IF;
    RETURN dot / (sqrt(mag_a) * sqrt(mag_b));
END;
$$ LANGUAGE plpgsql;