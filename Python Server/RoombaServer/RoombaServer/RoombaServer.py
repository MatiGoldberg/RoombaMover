# --- Roomba Server --- #
import thread
import socket
from logfile import logfile
from time import time, sleep
from struct import unpack
from RoombaLineParser import RoombaLineParser
import numpy as np

# -- Defines ------------------------------- #
VERSION = "V0.4"
WELCOME_MESSAGE = '--RoombaServer {0}--'.format(VERSION)
HOST_PORT = 8888

OUTPUT_PATH = '..\\..\\..\\Outputs\\'
SEQ_LOG_FILENAME = OUTPUT_PATH + 'Session_{0}.txt'
SERVER_LOG = OUTPUT_PATH + 'Server_Log.txt'
DATA_LOG = OUTPUT_PATH + 'Data_Log.txt'
XY_LOG = OUTPUT_PATH + 'XYlog.txt'
BUFFER_LENGTH = 1024
TIMEOUT_SEC = 3.3

# -- Globals ------------------------------- #
Lines = []
ServerLog = ''
DataLog = ''

#----COMMUNICATION-THREAD----------------------------------------------------------------------#
def OpenSocket():
	global ServerLog, Lines

	# (1) Configure Socket
	ServerLog.add_ts('Opening socket on port [{0}].'.format(str(HOST_PORT)))
	s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
	s.bind(('', HOST_PORT))
	s.listen(1)
	
	# (2) Open socket and listed to incoming connections. NO TIMEOUT.
	ServerLog.add_ts('Listening on port ' + str(HOST_PORT) + '...')
	print 'Listening on port ' + str(HOST_PORT) + '...'
	conn, addr = s.accept()
	
	# (3) Once a connection is made, SET TIMEOUT.
	print('Connected by', addr)
	ServerLog.add_ts('Connected by [{0}]'.format(addr))
	conn.settimeout(TIMEOUT_SEC)
	
	# (4) Receive data (in loop)
	while True:
		
		# (4a) Break conditions: timeout or bad data.
		try:
			data = conn.recv(BUFFER_LENGTH)
		except Exception as e:
			print '-Timeout Exception-'
			ServerLog.add_ts('-Timeout Exception-')
			break

		if not data:
			break
		
		# (4b) Update log
		print 'data received {' + data + '}'
		ServerLog.add_ts('Data received: {0}'.format(data))

		# (4c) Save to log
		#DataLog.add(data)
		Lines.append(data)
		conn.sendall('@OK')
		ServerLog.add_ts('Sent @OK.')
	
	conn.close()


def CommunicationThread(threadName, num):
    global ServerLog
    sleep(0.25)
    print threadName + ' started.'
    # Create two logs:
    ServerLog = logfile(SERVER_LOG)
    ServerLog.log_full_date()
    ServerLog.add_ts(WELCOME_MESSAGE)
    ServerLog.add_ts('Logs created.')
    
    while True:
    	OpenSocket()
#----------------------------------------------------------------------------------------------#

#----PARSER-THREAD-----------------------------------------------------------------------------#
def parse_header_line(line):
    SeqId = line.split('{')[1].split('}')[0]
    FW    = line.split('{')[2].split('}')[0]
    return SeqId, FW


def ParserThread(threadName, num):
    global Lines, DataLog, ServerLog
    print threadName + ' started.'
    DataLog = logfile(DATA_LOG)
    DataLog.log_full_date()

    XYLog = logfile(XY_LOG, True)
    x = 0.0
    y = 0.0
    Theta = 0.0
    S = 0.0

    SeqId= '0000'
    LastCounter = 0
    SeqLogger = logfile(SEQ_LOG_FILENAME.format(SeqId), True)

    while (1):
        if (len(Lines) ==  0):
            sleep(1)
            continue
        
        while (len(Lines) > 0):
        
            line = Lines.pop(0)
            DataLog.add_ts(line)
            
        
            # (1) Handle header lines
            if ('---' in line):
                SeqId, FW = parse_header_line(line)
                LastCounter = 0
                SeqLogger = logfile(SEQ_LOG_FILENAME.format(SeqId),True)
                ServerLog.add_ts("New sequence found: [{0}], FW [{1}].".format(SeqId, FW))
                continue
        
            # (2) Handle data lines
            line_object = RoombaLineParser(line)

            if (line_object.valid):
                SeqLogger.add(line_object.parse_sensors())
                ServerLog.add_ts("Parsed line [{0}].".format(line_object.counter_str))
                
                dS = line_object.dS
                dTheta = line_object.dTheta
                Theta = Theta + dTheta
                S = S + dS
                
                x = x + dS*np.cos(Theta)
                y = y + dS*np.sin(Theta)
                
                XYLog.add(str(x)+','+str(y)+','+str(line_object.Wall)+','+str(Theta)+','+str(S))
            else:
                ServerLog.add_ts("Invalid line [{0}]:\n\t\t{1}".format(line_object.counter_str,'\n\t\t'.join(line_object.error_log)))

            if (line_object.counter != LastCounter +1):
                ServerLog.add_ts("WARNING: Skipped line [{0}].".format(line_object.counter_str))

            LastCounter = line_object.counter;



#----------------------------------------------------------------------------------------------#

def main():
    print WELCOME_MESSAGE
    thread.start_new_thread(ParserThread, ("ParserThread", 2,))
    thread.start_new_thread(CommunicationThread, ("CommunicationThread", 1,))
    
    while(1):
        pass


if __name__ == "__main__":
	main()

