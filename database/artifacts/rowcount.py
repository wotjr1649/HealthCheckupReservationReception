import io, os, subprocess

os.chdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
q = """SET NOCOUNT ON;
SELECT '수검자|' + CONVERT(VARCHAR(10), COUNT(*)) FROM [dbo].[수검자];
SELECT '예약접수|' + CONVERT(VARCHAR(10), COUNT(*)) FROM [dbo].[예약접수];
SELECT '변경이력|' + CONVERT(VARCHAR(10), COUNT(*)) FROM [dbo].[변경이력];
SELECT '검사코드|' + CONVERT(VARCHAR(10), COUNT(*)) FROM [dbo].[검사코드];
SELECT '휴무일|' + CONVERT(VARCHAR(10), COUNT(*)) FROM [dbo].[휴무일];
SELECT '수검자표본|' + [차트번호] + '|' + [성명] FROM (SELECT TOP (12) [차트번호], [성명] FROM [dbo].[수검자] ORDER BY [수검자ID]) t;
"""
io.open('artifacts/_rc.sql', 'w', encoding='utf-8-sig', newline='').write(q)
subprocess.call(['sqlcmd', '-S', '.\\SQLEXPRESS', '-E', '-d', 'HealthCheckupReservationReceptionDb',
                 '-b', '-I', '-u', '-h-1', '-W', '-i', 'artifacts/_rc.sql', '-o', 'artifacts/_rc.out'])
raw = io.open('artifacts/_rc.out', 'rb').read().decode('utf-16')
for line in raw.split('\n'):
    line = line.strip()
    if '|' in line:
        print('  ' + line)
os.remove('artifacts/_rc.sql'); os.remove('artifacts/_rc.out')
