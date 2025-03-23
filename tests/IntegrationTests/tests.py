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
        print(f"\033[95m{received_msg}\033[0m")
        
        if received_msg.startswith("AUTH username AS Display_Name USING Abc-123-BCa"):
            response = "REPLY OK IS Auth success.\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")
        if received_msg.startswith("JOIN Channel AS Display_Name"):
            response = "REPLY OK IS JOIN success.\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")
        if received_msg.startswith("MSG FROM Display_Name IS MessageContent"):
            response = "MSG FROM Display_Name IS MessageContent\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")
        if received_msg.startswith("MSG FROM Display_Name IS BYE"):
            response = "BYE FROM SERVER\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")
        if received_msg.startswith("MSG FROM Display_Name IS ERROR"):
            response = "ERR FROM SERVER IS SOME ERROR ON SERVER SIDE\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")
    
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
        print(f"\033[95m{received_msg}\033[0m")
        
        if received_msg.startswith("AUTH username AS Display_Name USING Abc-123-BCa"):
            response = "REPLY NOK IS Auth unsuccess.\r\n"
            conn.sendall(response.encode())
            print(f"\033[36m{response.strip()}\033[0m")
    
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
class TestBYE(unittest.TestCase):
    def setUp(self):
        """This runs before each test to print the logo."""
        # Print the logo manually
        logo = """
        ################################################
        #              Test BYE                        #
        #              TestBYE                         #
        ################################################
        """
        print(logo)  # Print the logo with the test name
class TestAuth(unittest.TestCase):
    time.sleep(1)
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

    def test_authenticates_correctly_and_reports_success(self):
        """authenticates correctly and reports success"""
        print ("Running : test_authenticates_correctly_and_reports_success")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        # Run the client program and capture its output
        stdout, stderr = run_client_program()
        print(stdout)
        # Assert that the response contains the success message
        self.assertEqual("Action Success: Auth success.\n", stdout)

    def test_authenticates_incorrectly_and_reports_failure (self):
        """Test the failed authentication scenario."""
        print ("Running : test_authenticates_incorrectly_and_reports_failure")
        server_thread = threading.Thread(target=start_tcp_server_fail, daemon=True)
        server_thread.start()
        stdout, stderr = run_client_program()
        print(stdout)
        self.assertEqual("Action Failure: Auth unsuccess.\n", stdout)
    def test_authenticates_incorrectly_and_subsequently_correctly_and_reports (self):
        """Test the scenario where authentication first fails and then succeeds."""
        print ("Running : test_authenticates_incorrectly_and_subsequently_correctly_and_reports")
        # Start the server in failure mode
        server_thread_fail = threading.Thread(target=start_tcp_server_fail, daemon=True)
        server_thread_fail.start()
        stdout_fail, stderr_fail = run_client_program()
        print(stdout_fail)
        self.assertEqual("Action Failure: Auth unsuccess.\n", stdout_fail)
        
        # Start the server in success mode
        server_thread_success = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread_success.start()
        stdout_success, stderr_success = run_client_program()
        print(stdout_success)
        self.assertEqual("Action Success: Auth success.\n", stdout_success)

class TestMSG(unittest.TestCase):
    def setUp(self):
        time.sleep(1)
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
        print ("Running : test_msg_success")
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
        print ("Running : test_msg_many_success")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        
        time.sleep(0.1)  # Ensure the server starts before client
        
        # Run the client program and capture its output
        stdout, stderr = run_client_program("MessageContent")

        print(stdout)
        # Assert that the response contains the success message
        self.assertEqual("Action Success: Auth success.\nDisplay_Name: MessageContent\n", stdout)
class TestTermination(unittest.TestCase):
    def setUp(self):
        time.sleep(1)
        """Runs before each test to print the logo."""
        logo = """
        ################################################
        #              Test Termination                #
        #             TestTermination                  #
        ################################################
        """
        print(logo)  # Print the logo with the test name

    def test_terminates_on_eof(self):
        """[TCP] correctly terminates session upon reaching EOF"""
        print("Running: test_terminates_on_eof")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        run_client_program()
    def test_terminates_on_sigint(self):
        """[TCP] correctly terminates session upon SIGINT"""
        print("Running: test_terminates_on_sigint")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        run_client_program()
        
    def test_terminates_on_err(self):
        """[TCP] correctly terminates upon receiving ERR"""
        print("Running: test_terminates_on_err")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr = run_client_program("ERROR")
        print(stdout)
        self.assertEqual("Action Success: Auth success.\nERROR FROM SERVER: SOME ERROR ON SERVER SIDE\n", stdout)
    def test_terminates_on_bye(self):
        """[TCP] correctly terminates upon receiving BYE"""
        print("Running: test_terminates_on_bye")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr = run_client_program("BYE")
        print(stdout)
        self.assertEqual("Action Success: Auth success.\n", stdout)

if __name__ == "__main__":
    unittest.main()
