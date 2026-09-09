import io, os, subprocess

os.chdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
q = """SET NOCOUNT ON;
SELECT 'state|' + name + '|' + state_desc + '|' + user_access_desc
  FROM sys.databases WHERE name = 'HealthCheckupReservationReceptionDb';
SELECT 'session|' + CONVERT(VARCHAR(10), s.session_id) + '|' + ISNULL(s.program_name, '?')
     + '|' + ISNULL(s.host_name, '?') + '|' + s.status
  FROM sys.dm_exec_sessions s
  JOIN sys.databases d ON d.database_id = s.database_id
 WHERE d.name = 'HealthCheckupReservationReceptionDb' AND s.is_user_process = 1;
"""
io.open('artifacts/_st.sql', 'w', encoding='utf-8-sig', newline='').write(q)
subprocess.call(['sqlcmd', '-S', '.\\SQLEXPRESS', '-E', '-d', 'master',
                 '-b', '-I', '-u', '-h-1', '-W', '-i', 'artifacts/_st.sql', '-o', 'artifacts/_st.out'])
raw = io.open('artifacts/_st.out', 'rb').read().decode('utf-16', 'replace')
for line in raw.split('\n'):
    line = line.strip()
    if '|' in line:
        print('  ' + line)
os.remove('artifacts/_st.sql'); os.remove('artifacts/_st.out')
