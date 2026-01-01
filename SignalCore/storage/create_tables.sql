-- database: :memory:
PRAGMA foreign_keys = ON;

-- how original signal is created
-- blueprint how to create a signal generator
create TABLE if not EXISTS generations(
    id INTEGER PRIMARY KEY,
    object_factory_json TEXT,
    t_start REAL,
    t_end REAL,
    points INTEGER,
    amplitude real,
    frequency real,
    phase real
);

-- blueprint how to create a signal filter
CREATE TABLE if not EXISTS filters(
    id INTEGER PRIMARY KEY,
    object_factory_json TEXT
);

CREATE TABLE if not EXISTS transforms(
    id INTEGER PRIMARY KEY,
    object_factory_json TEXT
);

CREATE TABLE if not EXISTS normalizations(
    id INTEGER PRIMARY KEY,
    object_factory_json TEXT
);

CREATE TABLE if not EXISTS sessions(
    id INTEGER PRIMARY KEY,
    name varchar(256)
);

CREATE TABLE if not EXISTS signals_factory(
    id INTEGER PRIMARY KEY,
    name varchar(256),
    --programmable blueprint of signal creation
    expression text
);

-- each session can have multiple signals generated from
-- same signals_factory
CREATE TABLE if not EXISTS session_signals_instance(
    id INTEGER PRIMARY KEY,
    signal_name varchar(64),
    session_id INTEGER,
    factory_id INTEGER,
    signal_data BLOB,
    signal_metadata_json text,
    FOREIGN KEY (factory_id) REFERENCES signals_factory(id),
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);
CREATE TABLE if not EXISTS session_normalizations(
    id INTEGER PRIMARY KEY,
    var_name varchar(64),
    session_id INTEGER,
    normalization_id INTEGER,
    FOREIGN KEY (normalization_id) REFERENCES normalizations(id),
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);
CREATE TABLE if not EXISTS session_transforms(
    id INTEGER PRIMARY KEY,
    var_name varchar(64),
    session_id INTEGER,
    transform_id INTEGER,
    FOREIGN KEY (transform_id) REFERENCES transforms(id),
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);

CREATE TABLE if not EXISTS session_filters(
    id INTEGER PRIMARY KEY,
    var_name varchar(64),
    session_id INTEGER,
    filter_id INTEGER,
    FOREIGN KEY (filter_id) REFERENCES filters(id),
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);

CREATE TABLE if not EXISTS session_generations(
    id INTEGER PRIMARY KEY,
    var_name varchar(64),
    session_id INTEGER,
    generation_id INTEGER,
    FOREIGN KEY (generation_id) REFERENCES generations(id),
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);