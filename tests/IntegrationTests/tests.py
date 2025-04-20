import socket
import subprocess
import threading
import time
import signal
import unittest

def start_tcp_server_success(host='127.0.0.1', port=4567):
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
    process = subprocess.Popen(
        ['./ipk25chat-client', '-t', 'tcp', '-s', 'localhost'],
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        text=True
    )
    
    time.sleep(0.1)

    auth_command = "/auth username Abc-123-BCa Display_Name\n"
    process.stdin.write(auth_command)
    process.stdin.flush()

    time.sleep(0.1)

    if additional_command:
        process.stdin.write(additional_command + "\n")
        process.stdin.flush()

    time.sleep(0.1)

    stdout, stderr = process.communicate()
    exit_code = process.returncode
    return stdout, stderr, exit_code

class TestBYE(unittest.TestCase):
    def setUp(self):
        logo = """
        ################################################
        #              Test BYE                        #
        #              TestBYE                         #
        ################################################
        """
        print(logo)

class TestAuth(unittest.TestCase):
    time.sleep(1)
    def setUp(self):
        logo = """
        ################################################
        #              Test Authentication            #
        #                 TestAuth                    #
        ################################################
        """
        print(logo)

    def test_authenticates_correctly_and_reports_success(self):
        print("Running : test_authenticates_correctly_and_reports_success")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr, exit_code = run_client_program()
        print(stdout)
        self.assertEqual("Action Success: Auth success.\n", stdout)
        self.assertEqual(exit_code, 0)

    def test_authenticates_incorrectly_and_reports_failure(self):
        print("Running : test_authenticates_incorrectly_and_reports_failure")
        server_thread = threading.Thread(target=start_tcp_server_fail, daemon=True)
        server_thread.start()
        stdout, stderr, exit_code = run_client_program()
        print(stdout)
        self.assertEqual("Action Failure: Auth unsuccess.\n", stdout)
        self.assertEqual(exit_code, 0)

    def test_authenticates_incorrectly_and_subsequently_correctly_and_reports(self):
        print("Running : test_authenticates_incorrectly_and_subsequently_correctly_and_reports")
        server_thread_fail = threading.Thread(target=start_tcp_server_fail, daemon=True)
        server_thread_fail.start()
        stdout_fail, stderr_fail, exit_code_fail = run_client_program()
        print(stdout_fail)
        self.assertEqual("Action Failure: Auth unsuccess.\n", stdout_fail)

        server_thread_success = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread_success.start()
        stdout_success, stderr_success, exit_code_success = run_client_program()
        print(stdout_success)
        self.assertEqual("Action Success: Auth success.\n", stdout_success)
        self.assertEqual(exit_code_success, 0)

class TestMSG(unittest.TestCase):
    def setUp(self):
        time.sleep(1)
        logo = """
        ################################################
        #              Test Message                   #
        #                 TestMSG                     #
        ################################################
        """
        print(logo)

    def test_msg_success(self):
        print("Running : test_msg_success")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr, exit_code = run_client_program("MessageContent")
        print(stdout)
        self.assertEqual("Action Success: Auth success.\nDisplay_Name: MessageContent\n", stdout)
        self.assertEqual(exit_code, 0)

    def test_msg_many_success(self):
        print("Running : test_msg_many_success")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr, exit_code = run_client_program("MessageContent")
        print(stdout)
        self.assertEqual("Action Success: Auth success.\nDisplay_Name: MessageContent\n", stdout)
        self.assertEqual(exit_code, 0)

class TestTermination(unittest.TestCase):
    def setUp(self):
        time.sleep(1)
        logo = """
        ################################################
        #              Test Termination                #
        #             TestTermination                  #
        ################################################
        """
        print(logo)

    def test_terminates_on_eof(self):
        print("Running: test_terminates_on_eof")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr, exit_code = run_client_program()
        print(stdout)
        self.assertEqual(exit_code, 0)

    def test_terminates_on_sigint(self):
        print("Running: test_terminates_on_sigint")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr, exit_code = run_client_program()
        print(stdout)
        self.assertEqual(exit_code, 0)

    def test_terminates_on_err(self):
        print("Running: test_terminates_on_err")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr, exit_code = run_client_program("ERROR")
        print(stdout)
        self.assertEqual("Action Success: Auth success.\nERROR FROM SERVER: SOME ERROR ON SERVER SIDE\n", stdout)
        self.assertNotEqual(exit_code, 0)

    def test_terminates_on_bye(self):
        print("Running: test_terminates_on_bye")
        server_thread = threading.Thread(target=start_tcp_server_success, daemon=True)
        server_thread.start()
        stdout, stderr, exit_code = run_client_program("BYE")
        print(stdout)
        self.assertEqual("Action Success: Auth success.\n", stdout)
        self.assertEqual(exit_code, 0)

if __name__ == "__main__":
    unittest.main()
