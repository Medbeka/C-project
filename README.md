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
  
## Stage 3: Agent B and Master Aggregation
- Added AgentB project 
- Master now can get info from both agents using threads
- Aggregates word counts from both

## Stage 4: Multithread and CPU usage 
- Agents use separate threads for file reading and data sending.
- Master handles multiple named pipe connections concurrently with dedicated threads.
- Implemented CPU core affinity to assign each process Agent A, Agent B, and Master to a separate CPU core



