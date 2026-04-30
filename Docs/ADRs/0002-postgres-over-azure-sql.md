 # ADR-0002: Postgres over Azure SQL

  - Status: Accepted
  - Date: 2026-04-30                                                                                                                         
  - Related: PFL-009                                                                                                                            
  ## Context and Problem Statement                                                                                                              
                                                                                                                                                
  PharmaFlow needs an OLTP database for studies, sites, subjects, documents, signatures, and the audit trail. The repo runs on a $200 Azure     
  free-trial subscription, so anything above the cheapest tier is out.
                                                                                                                                                
  That leaves two real options, both Azure-native and supported by EF Core:

  * Postgres on Azure DB for PostgreSQL Flexible Server B1ms (~$13/mo)                                                                          
  * Azure SQL Database Basic, 5 DTU (~$5/mo)
                                                                                                                                                
  SQLite is also worth a mention but doesn't fit a regulated-software simulation.                                                               
   
  ## Considered Options                                                                                                                         
                                                        
  * Postgres on Azure DB for PostgreSQL Flexible Server B1ms; Postgres 17 in Docker locally                                                     
  * Azure SQL Database Basic, 5 DTU; SQL on Linux container locally (LocalDB doesn't exist on macOS)
  * SQLite — file-based, no Azure cost                                                                                                          
                                                                                                                                                
  ## Decision Outcome
                                                                                                                                                
  Postgres.                                             

  Cost wasn't the tiebreaker — SQL Basic is ~$8/mo cheaper. The reason is the Mac dev loop:                                                     
   
  - I'm on Apple Silicon. `docker run postgres` just works. Azure SQL on Linux runs under x64 emulation and it's slower.                        
  - `Testcontainers.PostgreSql` is reliable; the SQL Server one fights the test runner more than I want to deal with.
  - Same engine in Docker as in Azure, no dialect surprises at deploy time.                                                                     
                                                                                                                                                
  JSONB is a nice extra for `before_value`/`after_value` audit payloads but it's not the reason on its own.                                     
                                                                                                                                                
  SQLite is out: no real migration story, doesn't demonstrate the schema-discipline angle the project is supposed to show.                      
                                                        
  ## Consequences

  Good: local engine matches prod. Docker dev is fast on ARM64. JSONB and UUIDv7 (`gen_random_uuid()`) are native. Not locked into a            
  Microsoft-only data stack.
                                                                                                                                                
  Bad: lose Azure SQL temporal tables. That's actually fine — the audit pipeline is a hand-rolled hash-chained `AuditEvent` table (spec §13),   
  and that's the part of the project I want to show off. Building it myself is the point. Less Microsoft-ecosystem signal in interview optics;
  worth flagging, not enough to flip the call.                                                                                                  