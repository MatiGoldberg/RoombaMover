import datetime

class logfile:

	filename = ''

	def __init__(self, file_name, new_file = False):
		print 'Creating log file {' + file_name + '}'
		self.filename = file_name
		self.erase(new_file)

	def erase(self, your_sure):
		if (your_sure):
			file(self.filename,'w').write('')

	def write(self, text):
		file(self.filename,'a').write(text)

	def add(self, text):
		file(self.filename,'a').write(text + '\n')

	def add_ts(self, text):
		file(self.filename, 'a').write(self.ts() + text + '\n')

	def ts(self):
		timestamp = datetime.datetime.now().strftime("%H:%M:%S.%f")
		return '[' + timestamp[:-3] + '] '

	def log_full_date(self):
		pass
		self.add(datetime.datetime.now().strftime("--Logged at [%d-%m-%Y %H:%M:%S]--"))

