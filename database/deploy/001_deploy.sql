:On Error exit

USE Warehouse;
GO

-- create tables
:r .\schema\001_create_tenants.sql
:r .\schema\002_create_warehouses.sql
:r .\schema\003_create_unit_of_measure.sql
:r .\schema\004_create_products.sql
:r .\schema\005_create_warehouse_locations.sql
:r .\schema\006_create_inventory_lots.sql
:r .\schema\007_create_inventory_balances.sql
:r .\schema\008_create_inventory_transfer_statuses.sql
:r .\schema\009_create_inventory_transfers.sql
:r .\schema\010_create_inventory_transfer_lines.sql
:r .\schema\011_create_inventory_movements.sql
:r .\schema\012_create_states.sql
:r .\schema\013_create_warehouse_addresses.sql
:r .\schema\014_create_warehouse_contacts.sql

-- seed data

-- reference data
:r .\seed\001_seed_units_of_measure.sql
:r .\seed\002_seed_inventory_transfer_statuses.sql
:r .\seed\003_seed_address_regions.sql

-- scenario data
:r .\seed\004_seed_inventory_transfer_scenarios.sql
:r .\seed\005_seed_inventory_availability_scenarios.sql

GO
