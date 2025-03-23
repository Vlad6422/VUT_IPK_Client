import subprocess
import time

def run_program_with_delay(command, input_file, delay=0.5):
    # Open the process
    process = subprocess.Popen(command, stdin=subprocess.PIPE, text=True)
    
    # Read input file line by line
    with open(input_file, 'r') as file:
        for line in file:
            process.stdin.write(line)
            process.stdin.flush()
            time.sleep(delay)
    
    # Close stdin to signal EOF
    process.stdin.close()
    
    # Wait for process to complete
    process.wait()

if __name__ == "__main__":
    command = ["./ipk25-chat", "-t", "tcp", "-s", "anton5.fit.vutbr.cz"]
    input_file = "Scenarios/RENAME" # BYE JOIN MSG RENAME
    run_program_with_delay(command, input_file)