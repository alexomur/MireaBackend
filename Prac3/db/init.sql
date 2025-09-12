create extension if not exists pgcrypto;

create table if not exists users (
                                     id serial primary key,
                                     username text unique not null,
                                     password text not null
);

create table if not exists coffee (
                                      id serial primary key,
                                      name text not null,
                                      price_cents int not null check (price_cents > 0)
    );

create table if not exists orders (
                                      id serial primary key,
                                      customer_name text not null,
                                      coffee_id int not null references coffee(id),
    qty int not null check (qty > 0),
    created_at timestamptz not null default now()
    );

insert into users (username, password) values
    ('admin', 'admin123')
    on conflict (username) do update set password = excluded.password;

insert into coffee (name, price_cents) values
                                           ('Эспрессо', 150),
                                           ('Американо', 200),
                                           ('Капучино', 250),
                                           ('Латте', 300)
    on conflict do nothing;
