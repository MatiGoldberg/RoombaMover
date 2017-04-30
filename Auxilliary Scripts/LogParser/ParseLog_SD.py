from RoombaLineParser import RoombaLineParser
import datetime


LOG_FILE = 'Logs\\RoombaMapperLog.txt'
OUTPUT_FILE_TEMPLATE = 'SD\\SD_{0}.txt'

def TRACE(msg):
	timestamp = datetime.datetime.now().strftime("%H:%M:%S.%f")
	print '[' + timestamp[:-3] + '] ' + msg


def write_to_file(filename, lines):
	if (lines == []):
		return

	TRACE('Writing [{0}] lines to [{1}].'.format(len(lines), OUTPUT_FILE_TEMPLATE.format(filename)))
	file(OUTPUT_FILE_TEMPLATE.format(filename),'w').writelines(lines)


def parse_new_session(line):
	seq_id = line.split('{')[1].split('}')[0]
	fw = line.split('{')[2].split('}')[0]
	return seq_id, fw


def get_seconds(line):
	timestring = line.split('[')[1].split(']')[0]
	string_part = timestring.split(':')
	if (len(string_part) != 3):
		raise ValueError('bad timestring.')
	hours   = float(string_part[0])
	minutes = float(string_part[1])
	seconds = float(string_part[2])
	
	return 3600*hours + 60*minutes + seconds


def parse_logs_to_files(lines):
	TRACE('Parsing Logs...')
	raw_lines = []
	sensor_data = []
	seq_id = '0000'
	last_counter = 0
	for line in lines:
		# New Session Line
		if '---' in line:
			write_to_file(seq_id + '_raw', raw_lines)
			write_to_file(seq_id, sensor_data)
			seq_id, fw = parse_new_session(line)
			last_counter = 0
			TRACE('Found new session [{0}], FW [{1}].'.format(seq_id, fw))

		# Data Line
		else:
			split_line = line.split(' ')
			seconds = get_seconds(split_line[0])
			actual_line = split_line[1]
			line_obj = RoombaLineParser(actual_line)
			
			if (line_obj.valid):
				raw_lines.append(line_obj.textline)
				sensor_data.append(str(seconds) + ',' + line_obj.parse_sensors())
			else:
				TRACE('WARNING: Invalid line [{0}]:\n\t\t'.format(line_obj.counter) + '\n\t\t'.join(line_obj.error_log))

			if (line_obj.counter != last_counter+1):
				TRACE('WARNING: Skipped line(s): {0} [{1}].'.format(line_obj.counter, last_counter+1))
			
			last_counter = line_obj.counter

	write_to_file(seq_id + '_raw', raw_lines)
	write_to_file(seq_id, sensor_data)


def readfile():
	TRACE('Reading LOG_FILE.')
	try:
		lines = file(LOG_FILE,'r').readlines()
		return True, lines
	except:
		TRACE('ERROR: Could not find file.')
		return False, []


def main():
	TRACE('Running ParseLog_SD.py')

	success, lines = readfile()
	
	if (success):
		parse_logs_to_files(lines)

	TRACE('END.')


if __name__ == '__main__':
	main()