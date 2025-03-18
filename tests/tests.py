import socket
import subprocess
import threading
import time
import signal
import unittest

def start_tcp_server_success(host='127.0.0.1', port=4567):
    """Starts a TCP server that responds with a success message for a specific auth request."""
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind((host, port))
    server.listen(1)
    conn, addr = server.accept()
    while True:
        data = conn.recv(1024)
        if not data:
            break
        received_msg = data.decode().strip()
        print(f"\033[95m{received_msg}\033[0m")  # Pink text for server received packets
        
        if received_msg.startswith("AUTH username AS Display_Name USING Abc-123-BCa"):
            response = "REPLY OK IS Auth success.\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")  # Turquoise text for server sent packets
        if received_msg.startswith("JOIN Channel AS Display_Name"):
            response = "REPLY OK IS JOIN success.\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")  # Turquoise text for server sent packets
        if received_msg.startswith("MSG FROM Display_Name IS MessageContent"):
            response = "MSG FROM Display_Name IS MessageContent\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")  # Turquoise text for server sent packets
    
    conn.close()
    server.close()

def start_tcp_server_fail(host='127.0.0.1', port=4567):
    """Starts a TCP server that responds with a failure message for a specific auth request."""
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind((host, port))
    server.listen(1)
    conn, addr = server.accept()
    while True:
        data = conn.recv(1024)
        if not data:
            break
        received_msg = data.decode().strip()
        print(f"\033[95m{received_msg}\033[0m")  # Pink text for server received packets
        
        if received_msg.startswith("AUTH username AS Display_Name USING Abc-123-BCa"):
            response = "REPLY NOK IS Auth unsuccess.\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")  # Turquoise text for server sent packets
    
    conn.close()
    server.close()

def run_client_program(additional_command=None):
    """Runs the client program, sends authentication input, and optionally sends another command."""
    process = subprocess.Popen(
        ['./ipk25-chat', '-t', 'tcp', '-s', 'localhost'],
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        text=True
    )
    
    time.sleep(0.1)  # Give the server some time to start
    
    # First, send the authentication command
    auth_command = "/auth username Abc-123-BCa Display_Name\n"
    process.stdin.write(auth_command)
    process.stdin.flush()
    
    time.sleep(0.1)  # Allow time for the program to process the auth command
    
    # If there's an additional command, send it
    if additional_command:
        process.stdin.write(additional_command + "\n")
        process.stdin.flush()

    time.sleep(0.1)  # Allow time for the additional command to be processed
    
    # Read all stdout until the process finishes
    stdout, stderr = process.communicate()
    
    # Send Ctrl+C signal to terminate the process
    process.send_signal(signal.SIGINT)
    
    return stdout, stderr
class TestAuth(unittest.TestCase):
    def setUp(self):
        """This runs before each test to print the logo."""
        # Print the logo manually
        logo = """
        ################################################
        #              Test Authentication            #
        #                 TestAuth                    #
        ################################################
        """
        print(logo)  # Print the logo with the test name

    def test_auth_success(self):
        """Test the successful authentication scenario."""
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        # Run the client program and capture its output
        stdout, stderr = run_client_program()
        print(stdout)
        # Assert that the response contains the success message
        self.assertEqual("Action Success: Auth success.\n", stdout)

    def test_auth_failure(self):
        """Test the failed authentication scenario."""
        server_thread = threading.Thread(target=start_tcp_server_fail, daemon=True)
        server_thread.start()
        stdout, stderr = run_client_program()
        print(stdout)
        self.assertEqual("Action Failure: Auth unsuccess.\n", stdout)
class TestMSG(unittest.TestCase):
    def setUp(self):
        """This runs before each test to print the logo."""
        logo = """
        ################################################
        #              Test Message                   #
        #                 TestMSG                     #
        ################################################
        """
        print(logo)  # Print the logo with the test name

    def test_msg_success(self):
        """Test the successful authentication scenario."""
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        time.sleep(0.1)  # Ensure the server starts before client
        # Run the client program and capture its output
        stdout, stderr = run_client_program("MessageContent")
        print(stdout)
        # Assert that the response contains the success message
        self.assertEqual("Action Success: Auth success.\nDisplay_Name: MessageContent\n", stdout)
    def test_msg_many_success(self):
        """Test the successful authentication scenario."""
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        
        time.sleep(0.1)  # Ensure the server starts before client
        
        # Run the client program and capture its output
        stdout, stderr = run_client_program("MessageContent")

        print(stdout)
        # Assert that the response contains the success message
        self.assertEqual("Action Success: Auth success.\nDisplay_Name: MessageContent\n", stdout)


if __name__ == "__main__":
    unittest.main()
