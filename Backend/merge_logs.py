import glob
import csv
import re
import os

logfiles = glob.glob(r'.\logs\back_logs*.csv')
logfiles.sort()

datestr = re.findall('\((.*?)\).csv', logfiles[-1])[0]


with open(r'.\logs\logs ({}).csv'.format(datestr), 'w', newline='') as f:
    with open(logfiles[-1]) as bf:
        with open(r'.\logs\front_logs.csv') as ff:

            writer = csv.writer(f)
            b_reader = csv.reader(bf)
            f_reader = csv.reader(ff)

            b_log = None
            f_log = None
            while (1):
                if b_log is None:
                    try:
                        b_log = b_reader.__next__()
                    except StopIteration:
                        b_log = None
                if f_log is None:
                    try:
                        f_log = f_reader.__next__()
                    except StopIteration:
                        f_log = None

                if b_log is None and f_log is None:
                    break
            
                if f_log is None:
                    writer.writerow(b_log)
                    b_log = None
                elif b_log is None:
                    writer.writerow(f_log)
                    f_log = None
                elif b_log[0] <= f_log[0]:
                    writer.writerow(b_log)
                    b_log = None
                else:
                    writer.writerow(f_log)
                    f_log = None

os.remove(logfiles[-1])
os.remove(r'.\logs\front_logs.csv')