# C-project
# File Content Indexer – C# Project

# University Assignment  Distributed File Indexing with Agents and Master


# Stage 1: Agent A  Word Indexing from .txt Files
- Created a C# Console App  `AgentA`
- Reads `.txt` files from a given folder path 
- It counts how many times each word appears per file
- Prints result to the console
  
## stage 2: Master program was created
- Receives data from AgentA via named pipe "agentA"
- Displays each line received
  
## Stage 3: Agent B + Master Aggregation
- Added AgentB project 
- Master now can get info from both agents using threads
- Aggregates word counts from both


