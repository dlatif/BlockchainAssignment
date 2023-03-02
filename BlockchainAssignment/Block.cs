using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace BlockchainAssignment
{
    class Block
    {
        /* Block Variables */
        private DateTime timestamp; 

        private int index, // Position of the block in the sequence of blocks
            difficulty = 4;

        public String prevHash, // A reference pointer to the previous block
            hash, 
            merkleRoot,  // The merkle root of all transactions in the block
            minerAddress; // Public Key (Wallet Address) of the Miner

        public List<Transaction> transactionList; // List of transactions in this block

        //Task 6 part 1 - Multi-Threading
        public Boolean useThreading = true;
        public int threadOneNonce = 0;
        public int threadTwoNonce = 1;
        public string finalHash;
        public string threadOneFinalHash;
        public string threadTwoFinalHash;
        private bool threadOneDone = false;
        private bool threadTwoDone = false;

        // Proof-of-work
        public long nonce; 

        // Rewards
        public double reward; 

        // Genesis block constructor
        public Block()
        {
            timestamp = DateTime.Now;
            index = 0;
            transactionList = new List<Transaction>();
            //hash = Mine();
            if (useThreading == true)
            {
                this.mineThreaded();
                this.hash = this.finalHash;
            }
            else this.hash = this.CreateNewHash();

        }

        public Block(Block lastBlock, List<Transaction> TPool, string MinerAddress, int setting, string address)
        {
            this.transactionList = new List<Transaction>();
            this.nonce = 0;
            this.timestamp = DateTime.Now;
            this.difficulty = lastBlock.difficulty;
            this.adjustdiff(lastBlock.timestamp); 
            this.index = lastBlock.index + 1;
            this.prevHash = lastBlock.hash;
            this.minerAddress = MinerAddress;
            TPool.Add(createRewardTransaction(TPool)); // Create the reward transaction
            this.addFromPool(TPool, setting, address);
            // this.hash = this.CreateNewMine();    //    Create hash from index, prevhash and time
            this.merkleRoot = MerkleRoot(transactionList); // Calculate the merkle root of the blocks transactions

            if (useThreading == true)
            {
                this.mineThreaded();
                this.hash = this.finalHash;
            }
            else this.hash = this.CreateNewMine();
        }











        /* New Block constructor */
        
        /*
        public Block(Block lastBlock, List<Transaction> transactions, String minerAddress)
        {
            timestamp = DateTime.Now;

            index = lastBlock.index + 1;
            prevHash = lastBlock.hash;

            this.minerAddress = minerAddress; // The wallet to be credited the reward for the mining effort
            reward = 1.0; // Assign a simple fixed value reward
            transactions.Add(createRewardTransaction(transactions)); // Create and append the reward transaction
            transactionList = new List<Transaction>(transactions); // Assign provided transactions to the block

            merkleRoot = MerkleRoot(transactionList); // Calculate the merkle root of the blocks transactions


            hash = Mine(); // Conduct PoW to create a hash which meets the given difficulty requirement
        }
        */

        /* Hashes the entire Block object */
        public String CreateHash()
        {
            String hash = String.Empty;
            SHA256 hasher = SHA256Managed.Create();

            String input = timestamp.ToString() + index + prevHash + nonce + merkleRoot;

            /* Apply the hash function to the block as represented by the string "input" */
            Byte[] hashByte = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

            /* Reformat to a string */
            foreach (byte x in hashByte)
                hash += String.Format("{0:x2}", x);
            
            return hash;
        }

        // Create a Hash which satisfies the difficulty level required for Proof of Work
        public String Mine()
        {
            nonce = 0; // Initalise the nonce
            String hash = CreateHash(); // Hash the block

            String re = new string('0', difficulty); // A string for analysing the PoW requirement

            while(!hash.StartsWith(re)) // Check the resultant hash against the "re" string
            {
                nonce++; // Increment the nonce should the difficulty level not be satisfied
                hash = CreateHash(); // Rehash with the new nonce as to generate a different hash
            }

            return hash; // Return the hash meeting the difficulty requirement
        }

        // Merkle Root Algorithm
        public static String MerkleRoot(List<Transaction> transactionList)
        {
            List<String> hashes = transactionList.Select(t => t.hash).ToList(); 
            
            if (hashes.Count == 0) 
            {
                return String.Empty;
            }
            if (hashes.Count == 1) 
            {
                return HashCode.HashTools.combineHash(hashes[0], hashes[0]);
            }
            while (hashes.Count != 1) 
            {
                List<String> merkleLeaves = new List<String>(); 

                for (int i=0; i<hashes.Count; i+=2) 
                {
                    if (i == hashes.Count - 1)
                    {
                        merkleLeaves.Add(HashCode.HashTools.combineHash(hashes[i], hashes[i])); 
                    }
                    else
                    {
                        merkleLeaves.Add(HashCode.HashTools.combineHash(hashes[i], hashes[i + 1])); 
                    }
                }
                hashes = merkleLeaves; 
            }
            return hashes[0]; 
        }

        // Create reward for incentivising the mining of block
        public Transaction createRewardTransaction(List<Transaction> transactions)
        {
            double fees = transactions.Aggregate(0.0, (acc, t) => acc + t.fee); // Sum all transaction fees
            return new Transaction("Mine Rewards", minerAddress, (reward + fees), 0, ""); // Issue reward as a transaction in the new block
        }

        /* Concatenate all properties to output to the UI */
        public override string ToString()
        {
            return "[BLOCK START]"
                + "\nIndex: " + index
                + "\tTimestamp: " + timestamp
                + "\nPrevious Hash: " + prevHash
                + "\n-- PoW --"
                + "\nDifficulty Level: " + difficulty
                + "\nNonce: " + nonce
                + "\nHash: " + hash
                + "\n-- Rewards --"
                + "\nReward: " + reward
                + "\nMiners Address: " + minerAddress
                + "\n-- " + transactionList.Count + " Transactions --"
                +"\nMerkle Root: " + merkleRoot
                + "\n" + String.Join("\n", transactionList)
                + "\n[BLOCK END]";
        }



        //FOR THREADING
        

        public String CreateNewHash(int inNonce)
        {
            SHA256 hasher;
            hasher = SHA256Managed.Create();
            String input = this.index.ToString() + this.timestamp.ToString() + this.prevHash + inNonce + this.merkleRoot + this.reward.ToString();
            Byte[] hashByte = hasher.ComputeHash(Encoding.UTF8.GetBytes((input)));
            String hash = string.Empty;

            foreach (byte x in hashByte)
            {
                hash += String.Format("{0:x2}", x);
            }
            return hash;
        }

        public string CreateNewHash()
        { 
            SHA256 hasher;
            hasher = SHA256Managed.Create();
            String input = this.index.ToString() + this.timestamp.ToString() + this.prevHash + this.nonce + this.merkleRoot + this.reward.ToString();
            Byte[] hashByte = hasher.ComputeHash(Encoding.UTF8.GetBytes((input)));

            String hash = string.Empty;

            foreach (byte x in hashByte)
            {
                hash += String.Format("{0:x2}", x);
            }
            return hash;
        }

        private string CreateNewMine()
        {
            string hash = ""; 
            string diffString = new string('0', this.difficulty);
            while (hash.StartsWith(diffString) == false)
            {
                hash = this.CreateNewHash();
                this.nonce++;
            }
            this.nonce--;
            if (hash is null)
            {
                throw new IndexOutOfRangeException("No hash generated");
            }
            return hash;
        }


        public void mineThreaded()
        {
            Thread th1 = new Thread(this.mineThreadOne);
            Thread th2 = new Thread(this.mineThreadTwo);

            th1.Start();
            th2.Start();

            while (th1.IsAlive == true || th2.IsAlive == true) { Thread.Sleep(1); }

            if (this.threadTwoFinalHash is null)
            {
                this.nonce = this.threadOneNonce;
                this.finalHash = this.threadOneFinalHash;
            }
            else
            {
                this.nonce = this.threadTwoNonce;
                this.finalHash = this.threadTwoFinalHash;
            }
            if (this.finalHash is null)
            {
                Console.WriteLine(this.ToString());
                throw new Exception("NULL finalhash" +
                    " Thread 1 Nonce: " + this.threadOneNonce +
                    " Thread 2 Nonce: " + this.threadTwoNonce +
                    " Nonce: " + this.nonce +
                    " threadOneFinalHash " + this.threadOneFinalHash +
                    " threadTwoFinalHash: " + this.threadTwoFinalHash +
                    " NewHash: " + this.CreateNewHash());
            }
        }

        public void mineThreadOne()
        {
            threadOneDone = false;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Boolean check = false;
            String newHash;
            string diffString = new string('0', this.difficulty);

            while (check == false)
            {
                newHash = CreateNewHash(this.threadOneNonce);
                if (newHash.StartsWith(diffString) == true)
                {
                    check = true;
                    this.threadOneFinalHash = newHash;
                    Console.WriteLine("Block index: " + this.index);
                    Console.WriteLine("Thread 1 done: Thread 1 next: " + this.threadOneFinalHash);
                    threadOneDone = true;

                    Console.WriteLine(threadOneNonce);
                    sw.Stop();
                    Console.WriteLine("Thread 1 time:");
                    Console.WriteLine(sw.Elapsed);

                    return;
                }
                else if (threadTwoDone == true)
                {
                    Console.WriteLine("Thread 1 done. Thread 2 next: " + this.threadTwoFinalHash);
                    Thread.Sleep(1);
                    return;
                }
                else
                {
                    check = false;
                    this.threadOneNonce += 2;
                }
            }
            return;
        }

        public void mineThreadTwo()
        {
            threadTwoDone = false;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Boolean check = false;
            String newHash;
            string diffString = new string('0', this.difficulty);
            while (check == false)
            {
                newHash = CreateNewHash(this.threadTwoNonce);
                if (newHash.StartsWith(diffString) == true)
                {
                    check = true;
                    this.threadTwoFinalHash = newHash;
                    Console.WriteLine("Block index: " + this.index);
                    Console.WriteLine("Thread 2 done. Thread 2 next: " + this.threadTwoFinalHash);
                    threadTwoDone = true;

                    Console.WriteLine(this.threadTwoNonce);
                    sw.Stop();
                    Console.WriteLine("Thread 2 time:");
                    Console.WriteLine(sw.Elapsed);

                    return;
                }
                else if (threadOneDone == true)
                {
                    Console.WriteLine("Thread 2 done. Thread 1 next: " + this.threadOneFinalHash);
                    Thread.Sleep(1);
                    return;
                }
                else
                {
                    check = false;
                    this.threadTwoNonce += 2;
                }
            }
            return;
        }

        public string ReturnString()
        {
            return ("\n\n\t\t[BLOCK START]"
                + "\nIndex: " + this.index
                + "\tTimestamp: " + this.timestamp
                + "\nPrevious Hash: " + this.prevHash
                + "\n\t\t-- PoW --"
                + "\nDifficulty Level: " + this.difficulty
                + "\nNonce: " + this.nonce
                + "\nHash: " + this.hash
                + "\n\t\t-- Rewards --"
                + "\nReward: " + this.reward
                + "\nMiners Address: " + this.minerAddress
                + "\n\t\t-- " + this.transactionList.Count + " Transactions --"
                + "\nMerkle Root: " + this.merkleRoot
                + "\n" + String.Join("\n", this.transactionList)
                + "\n\t\t[BLOCK END]");

        }


        //ADJUSTING DIFFICULTY
        public void adjustdiff(DateTime lastTime)
        {
            //time how long it takes to generate blocks
            DateTime startTime = DateTime.UtcNow;
            TimeSpan timeDiff = startTime - lastTime;

            //If the time is less than 10 seconds, increase the difficulty
            if (timeDiff < TimeSpan.FromSeconds(10))
            {
                this.difficulty++;
                Console.WriteLine("Time since last block- " + timeDiff);
                Console.WriteLine("Difficulty increased to: " + this.difficulty);
            }
            //If the time is more than 10 seconds, decrease the difficulty
            else if (timeDiff > TimeSpan.FromSeconds(5))
            {
                difficulty--;
                Console.WriteLine("Time since last block- " + timeDiff);
                Console.WriteLine("Difficulty decreased to: " + this.difficulty);
            }

            //increasing difficulty makes time exponential, so allow no higher than 6
            if (this.difficulty >= 6)
            {
                this.difficulty = 4;
                Console.WriteLine("High difficulty! Now set to: " + this.difficulty);
            }

            //Because difficulty is dynamic, it may auto set to lower than 0. which is not intended
            else if (this.difficulty <= 0)
            {
                this.difficulty = 0;
                Console.WriteLine("Difficulty cannont be < 0. Now set to: " + this.difficulty);
            }
        }

        //MINING SETTINGS
        public void addFromPool(List<Transaction> transactionPool, int selection, string address)
        {
            int max = 5;
            int idToMine = 0;

            while (transactionList.Count < max && transactionPool.Count > 0)
            {
                if (selection == 0)
                {  
                    //Random     
                    Random random = new Random();
                    idToMine = random.Next(0, transactionPool.Count);
                    this.transactionList.Add(transactionPool.ElementAt(idToMine));
                }
                else if (selection == 1)
                {
                    //Altruistic
                    for (int i = 0; ((i < transactionPool.Count) && (i < max)); i++)
                    {
                        this.transactionList.Add(transactionPool.ElementAt(i));
                    }
                }
                else if (selection == 2)
                {
                    //Greedy
                    for (int i = 0; ((i < transactionPool.Count)); i++)
                    {
                        if (transactionPool.ElementAt(i).fee > transactionPool.ElementAt(idToMine).fee)
                        {
                            idToMine = i;
                        }
                    }
                    this.transactionList.Add(transactionPool.ElementAt(idToMine));
                }
                else if (selection == 3)
                {
                    //By specified address
                    for (int i = 0; i < transactionPool.Count && (transactionList.Count < max); i++)
                    {
                        if (transactionPool.ElementAt(i).senderAddress == address)
                        {
                            this.transactionList.Add(transactionPool.ElementAt(i));
                        }
                        else if (transactionPool.ElementAt(i).recipientAddress == address)
                        {
                            this.transactionList.Add(transactionPool.ElementAt(i));
                        }
                        else
                        {
                            Console.WriteLine("Error: Can't mine this address");
                        }
                    }
                }
                else
                {
                    //If nothing is specified, pick random
                    selection = 0;
                }
                transactionPool = transactionPool.Except(this.transactionList).ToList();
            }
        }




    }
}
