using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OpBlockController
{
    private Operator _owner;

    // 저지 가능 후보자들. blockedEnemies도 포함됨.
    // 주의) Queue로 구현하면 인덱스 중간에 있는 적이 먼저 제거되는 경우를 처리할 수 없음
    private List<Enemy> blockableEnemies = new List<Enemy>(); 
    private List<Enemy> blockedEnemies = new List<Enemy>(); // 실제로 저지 중인 적들
    private int currentBlockCount; // 현재 저지 중인 적의 수

    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies;
    public IReadOnlyList<Enemy> BlockableEnemies => blockableEnemies; 

    public void Initialize(Operator op)
    {
        _owner = op;
    }

    public void OnEnemyEnteredBlockRange(Enemy enemy)
    {
        if (!blockableEnemies.Contains(enemy))
        {
            // 저지 가능 리스트에 추가
            blockableEnemies.Add(enemy);

            // 리스트 내의 적들에 대해 저지 시도
            TryBlockNextEnemy();
        }
    }

    public void OnEnemyExitedBlockRange(Enemy enemy)
    {
        // 저지 가능 리스트에서 제거
        blockableEnemies.Remove(enemy);

        // 실제 저지 중이었다면 저지 해제
        if (blockedEnemies.Contains(enemy))
        {
            UnblockEnemy(enemy);
            enemy.UpdateBlockingOperator(null);
        }

        // 리스트 내의 적들에 대해 저지 시도
        TryBlockNextEnemy();
    }

    // 저지 가능한 슬롯이 있을 때 blockableEnemies에서 다음 적을 찾아 저지한다.
    // 저지 가능 / 저지된 적에 변동이 생길 때 함께 실행함
    private void TryBlockNextEnemy()
    {
        // 여유가 없다면 리턴
        if (currentBlockCount >= _owner.MaxBlockableEnemies) return;

        // 저지 가능한 적 목록을 순회, 리스트 앞쪽 = 먼저 들어온 적
        foreach (Enemy candidateEnemy in blockableEnemies)
        {
            // 이 적을 저지할 수 있는지 확인
            if (CanBlockEnemy(candidateEnemy))
            {
                BlockEnemy(candidateEnemy);
                candidateEnemy.UpdateBlockingOperator(_owner);
            }
        }
    }

    // 해당 적을 저지할 수 있는가
    protected bool CanBlockEnemy(Enemy enemy)
    {
        return enemy != null && 
            _owner.IsDeployed &&
            !blockedEnemies.Contains(enemy) && // 이미 저지 중인 적이 아님
            enemy.BlockingOperator == null &&  // 적을 저지하고 있는 오퍼레이터가 없음
            currentBlockCount + enemy.BlockSize <= _owner.MaxBlockableEnemies; // 이 적을 저지했을 때 최대 저지 수를 초과하지 않음
    }    

    public void BlockEnemy(Enemy enemy)
    {
        blockedEnemies.Add(enemy);
        currentBlockCount += enemy.BlockSize;
    }

    public void UnblockEnemy(Enemy enemy)
    {
        blockedEnemies.Remove(enemy);
        currentBlockCount -= enemy.BlockSize;
    }

    public void UnblockAllEnemies()
    {
        // ToList를 쓰는 이유 : 반복문 안에서 리스트를 수정하면 오류가 발생할 수 있다 - 복사본을 순회하는 게 안전하다.
        foreach (Enemy enemy in blockedEnemies.ToList())
        {
            enemy.UpdateBlockingOperator(null);
            UnblockEnemy(enemy);
        }
    }

    public void ResetStates()
    {
        blockedEnemies.Clear();
        blockableEnemies.Clear();
        currentBlockCount = 0;
    }
}